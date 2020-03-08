package db

import (
	"crypto/rand"
	"fmt"
	"os"

	"golang.org/x/crypto/bcrypt"
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

type Players interface {
	CreatePlayer(name, pass string) error
	CheckPassword(name, pass string) error
}

type Player struct {
	ID   int    `json:"id"`
	Name string `json:"name"`
	Hash string `json:"-"`
	Salt string `json:"-"`
}

func (p *Player) SetPassword(password string) error {
	p.Salt = cryptostring(12)
	cat := []byte(password + p.Salt)
	hashBytes, err := bcrypt.GenerateFromPassword(cat, 13)
	if err != nil {
		return fmt.Errorf("unable to generate password hash: %w", err)
	}
	p.Hash = string(hashBytes)
	return nil
}

func (p *Player) HasPassword(password string) bool {
	cat := []byte(password + p.Salt)
	if err := bcrypt.CompareHashAndPassword([]byte(p.Hash), cat); err != nil {
		if err != bcrypt.ErrMismatchedHashAndPassword {
			fmt.Fprintf(os.Stderr, "error checking password for %v: %v\n", p, err)
		}
		return false
	}
	return true
}

func (p Player) String() string {
	return fmt.Sprintf(`Player{ID: %d, Name: "%s"}`, p.ID, p.Name)
}
