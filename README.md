# Music 

## About
Music app uses libspotify to play spotify music.  It set ups a play queue and supports the following commands:
* Mycroft [play, pause, next, clear queue]
* Mycroft [play, add] the [song, album] [song/album name]

## Caveats
1. We haven't gotten this to work with speakers app yet so it just plays the through NAudio in the music app. This can't be a permanent solution because NAudio is not cross platform as well as the fact that we should using speakers app for audio out. Hopefully we can get this to work.
2. It uses libspotify, to use the app, a spotify premium account and an API key is required to use it. 
3. It uses dictation grammar for the parsing the song album and name. Dictation grammar is pretty bad. 
