package main

import (
	"crypto/rand"
	"fmt"
	"net"
	"net/http"
	"os"

	"github.com/jordanorelli/blammo"
	"github.com/jordanorelli/kloam/db"
	"github.com/spf13/cobra"
)

func cryptostring(n int) string {
	b := make([]byte, n)
	rand.Read(b)
	letters := `abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%*-=+`
	r := make([]byte, 0, n)
	for _, n := range b {
		r = append(r, letters[int(n)%len(letters)])
	}
	return string(r)
}

func runServer(cmd *cobra.Command, args []string) {
	stdout := blammo.NewLineWriter(os.Stdout)
	stderr := blammo.NewLineWriter(os.Stderr)
	log := blammo.NewLog("kloam", blammo.DebugWriter(stdout), blammo.InfoWriter(stdout), blammo.ErrorWriter(stderr))

	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		log.Error("unable to open sqlite database: %v", err)
		os.Exit(1)
	}
	defer conn.Close()

	s := server{
		Log:   log,
		db:    conn,
		join:  make(chan player),
		leave: make(chan *player),
		inbox: make(chan message),
		souls: make(map[string]soul),
	}
	s.init()
	go s.run()
	lis, err := net.Listen("tcp", cmd.Flag("listen").Value.String())
	if err != nil {
		log.Error("listen error: %v", err)
		return
	}
	log.Info("listening on %v", lis.Addr())
	http.Serve(lis, s.handler())
}

func runUserCreate(cmd *cobra.Command, args []string) {
	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		fmt.Fprintf(os.Stderr, "unable to open sqlite database: %v\n", err)
	}
	defer conn.Close()

	user := args[0]
	var pass string
	if len(args) > 1 {
		pass = args[1]
	} else {
		pass = cryptostring(12)
	}
	salt := cryptostring(12)

	if err := conn.CreateUser(user, pass, salt); err != nil {
		fmt.Fprintf(os.Stderr, "failed to create user: %v\n", err)
		return
	}

	fmt.Printf("created:\n\tuser:\t%s\n\tpass:\t%s\n", user, pass)
}

func runUserCheckPassword(cmd *cobra.Command, args []string) {
	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		fmt.Fprintf(os.Stderr, "unable to open sqlite database: %v\n", err)
	}
	defer conn.Close()

	user := args[0]
	pass := args[1]
	if err := conn.CheckPassword(user, pass); err != nil {
		fmt.Fprintf(os.Stderr, "failed password check: %v\n", err)
	}
}

func main() {
	cmd := &cobra.Command{
		Use: "kloam",
	}
	cmd.PersistentFlags().String("db", "./kloam.sqlite3", "path to a sqlite3 file to use as a database")

	server := &cobra.Command{
		Use:   "server",
		Short: "the kloam multiplayer server",
		Run:   runServer,
	}
	server.Flags().StringP("listen", "l", "0.0.0.0:9001", "ip:port to listen on")
	cmd.AddCommand(server)

	user := &cobra.Command{
		Use:   "user",
		Short: "user management stuff",
	}
	cmd.AddCommand(user)

	userCreate := &cobra.Command{
		Use:   "create",
		Short: "create a user",
		Args:  cobra.RangeArgs(1, 2),
		Run:   runUserCreate,
	}
	user.AddCommand(userCreate)

	userCheckPassword := &cobra.Command{
		Use:   "check-password",
		Short: "checks a users password",
		Args:  cobra.ExactArgs(2),
		Run:   runUserCheckPassword,
	}
	user.AddCommand(userCheckPassword)

	cmd.Execute()
}
