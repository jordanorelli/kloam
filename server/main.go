package main

import (
	"net"
	"net/http"
	"os"

	"github.com/jordanorelli/blammo"
)

func main() {
	stdout := blammo.NewLineWriter(os.Stdout)
	stderr := blammo.NewLineWriter(os.Stderr)
	log := blammo.NewLog("kloam", blammo.DebugWriter(stdout), blammo.InfoWriter(stdout), blammo.ErrorWriter(stderr))
	log.Info("this is some info")
	log.Info("some more info here")

	s := server{
		Log: log,
	}
	s.init()
	lis, err := net.Listen("tcp", "0.0.0.0:9001")
	if err != nil {
		log.Error("listen error: %v", err)
		return
	}
	http.Serve(lis, s.handler())
}
