package db

type Users interface {
	CreateUser(name, pass string) error
	CheckPassword(name, pass string) error
}
