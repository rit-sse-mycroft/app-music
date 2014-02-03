# Music

## About
Music app uses libspotify to play spotify music.  It set ups a play queue and supports the following commands:
* Mycroft [play, pause, next, clear queue]
* Mycroft [play, add] the [song, album] [song/album name]

## Caveats
1. It uses libspotify so to use the app, a spotify premium account and an API key is required.
2. It uses dictation grammar for the parsing the song album and name. Dictation grammar is pretty bad.
