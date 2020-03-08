package db

type Players interface {
	CreatePlayer(name, pass string) error
	CheckPassword(name, pass string) error
}
