package main

import (
	"net"
	"net/http"
	"os"

	"github.com/jordanorelli/blammo"
	"github.com/spf13/cobra"
)

func main() {
	cmd := &cobra.Command{
		Use: "kloam",
	}

	server := &cobra.Command{
		Use:   "server",
		Short: "the kloam multiplayer server",
		Run: func(cmd *cobra.Command, args []string) {
			stdout := blammo.NewLineWriter(os.Stdout)
			stderr := blammo.NewLineWriter(os.Stderr)
			log := blammo.NewLog("kloam", blammo.DebugWriter(stdout), blammo.InfoWriter(stdout), blammo.ErrorWriter(stderr))

			s := server{
				Log:   log,
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
		},
	}
	server.Flags().StringP("listen", "l", "0.0.0.0:9001", "ip:port to listen on")

	cmd.AddCommand(server)
	cmd.Execute()

}
