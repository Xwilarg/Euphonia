package com.example.euphonia.data

data class Song(
    val name: String,
    val path: String,
    val artist: String?,
    val playlist: String,
    val album: String?,
    val source: String?,
    val type: String?,
    val isArchived: Boolean
)

data class ExtendedSong(
    val song: Song,
    val album: Album?
)