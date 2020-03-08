package main

import (
	"crypto/rand"
	"database/sql"
	"errors"
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

func runPlayerCreate(cmd *cobra.Command, args []string) {
	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		fmt.Fprintf(os.Stderr, "unable to open sqlite database: %v\n", err)
	}
	defer conn.Close()

	player := db.Player{Name: args[0]}
	var pass string
	if len(args) > 1 {
		pass = args[1]
	} else {
		pass = cryptostring(12)
	}
	player.SetPassword(pass)

	if err := conn.CreatePlayer(&player); err != nil {
		fmt.Fprintf(os.Stderr, "failed to create player: %v\n", err)
		return
	}

	fmt.Printf("created:\n\tid:\t%d\n\tplayer:\t%s\n\tpass:\t%s\n", player.ID, player.Name, pass)
}

func runPlayerCheckPassword(cmd *cobra.Command, args []string) {
	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		fmt.Fprintf(os.Stderr, "unable to open sqlite database: %v\n", err)
	}
	defer conn.Close()

	player := db.Player{Name: args[0]}
	if err := conn.ReadPlayer(&player); err != nil {
		fmt.Fprintf(os.Stderr, "unable to fetch player row: %v\n", err)
		return
	}

	pass := args[1]
	if !player.HasPassword(pass) {
		fmt.Fprintf(os.Stderr, "bad password\n", err)
	}
}

func runPlayerSetPassword(cmd *cobra.Command, args []string) {
	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		fmt.Fprintf(os.Stderr, "unable to open sqlite database: %v\n", err)
	}
	defer conn.Close()

	player := db.Player{Name: args[0]}
	if err := conn.ReadPlayer(&player); err != nil {
		fmt.Fprintf(os.Stderr, "unable to read player record: %v\n", err)
		return
	}

	if err := player.SetPassword(args[1]); err != nil {
		fmt.Fprintf(os.Stderr, "unable to set player password: %v\n", err)
		return
	}

	if err := conn.UpdatePlayer(&player); err != nil {
		fmt.Fprintf(os.Stderr, "unable to save player changes: %v\n", err)
	}
}

func runPlayerStatus(cmd *cobra.Command, args []string) {
	conn, err := db.OpenSQLite(cmd.Flag("db").Value.String())
	if err != nil {
		fmt.Fprintf(os.Stderr, "unable to open sqlite database: %v\n", err)
	}
	defer conn.Close()

	player := db.Player{Name: args[0]}
	if err := conn.ReadPlayer(&player); err != nil {
		fmt.Fprintf(os.Stderr, "unable to read player record: %v\n", err)
		return
	}

	body := db.Body{PlayerID: player.ID}
	err = conn.ReadBody(&body)
	switch {
	case errors.Is(err, sql.ErrNoRows):
		fmt.Println("alive")
		break
	case err == nil:
		fmt.Printf("dead since %v\n", body.DiedAt)
		break
	default:
		fmt.Fprintf(os.Stderr, "unable to query for bodies: %v\n", err)
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

	player := &cobra.Command{
		Use:   "player",
		Short: "player management stuff",
	}
	cmd.AddCommand(player)

	playerCreate := &cobra.Command{
		Use:   "create",
		Short: "create a player",
		Args:  cobra.RangeArgs(1, 2),
		Run:   runPlayerCreate,
	}
	player.AddCommand(playerCreate)

	playerCheckPassword := &cobra.Command{
		Use:   "check-password",
		Short: "checks a player's password",
		Args:  cobra.ExactArgs(2),
		Run:   runPlayerCheckPassword,
	}
	player.AddCommand(playerCheckPassword)

	playerSetPassword := &cobra.Command{
		Use:   "set-password",
		Short: "sets a player's password",
		Args:  cobra.ExactArgs(2),
		Run:   runPlayerSetPassword,
	}
	player.AddCommand(playerSetPassword)

	playerStatus := &cobra.Command{
		Use:   "status",
		Short: "gets a player's status",
		Args:  cobra.ExactArgs(1),
		Run:   runPlayerStatus,
	}
	player.AddCommand(playerStatus)

	cmd.Execute()
}
