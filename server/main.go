package main

import (
	"os"

	"github.com/jordanorelli/blammo"
)

func main() {
	stdout := blammo.NewLineWriter(os.Stdout)
	stderr := blammo.NewLineWriter(os.Stderr)
	log := blammo.NewLog("kloam", blammo.DebugWriter(stdout), blammo.InfoWriter(stdout), blammo.ErrorWriter(stderr))
	log.Info("this is some info")
	log.Info("some more info here")
}
