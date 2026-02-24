package eu.zirk.euphonia.data

data class Song(
    val name: String,
    val path: String,
    val artist: String?,
    val playlists: Array<String>,
    val source: String?,
    val type: String?,
    val isArchived: Boolean,
    val key: String?,
    val albumName: String?,
    val thumbnailHash: String?,

    // Deprecated
    val album: String?,
)

data class ExtendedSong(
    val song: Song,
    val imagePath: String?
)