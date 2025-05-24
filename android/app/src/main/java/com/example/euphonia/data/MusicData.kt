package com.example.euphonia.data

data class MusicData(
    var musics: Array<Song>,
    val highlight: Array<String>,
    val playlists: Map<String, Playlist>?,
    val albums: Map<String, Album>
)
