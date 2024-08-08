package com.example.euphonia.ui

import android.content.Context
import android.net.Uri
import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.ListView
import androidx.activity.OnBackPressedCallback
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.preference.PreferenceManager
import com.example.euphonia.R
import com.example.euphonia.SongAdapter
import com.example.euphonia.data.ExtendedSong
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Song
import com.example.euphonia.MainActivity
import kotlin.random.Random

class MusicFragment : Fragment() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
    }


    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {

        val view =  inflater.inflate(R.layout.fragment_music, container, false)
        pView = requireActivity() as MainActivity

        requireActivity()
            .onBackPressedDispatcher
            .addCallback(this, object : OnBackPressedCallback(true) {
                override fun handleOnBackPressed() {
                    if (pView.currentPlaylist != null) {
                        pView.currentPlaylist = null
                        updateList()
                    } else {
                        requireActivity().onBackPressed()
                    }
                }
            })

        list = view.findViewById(R.id.musicData)
        // Callback when we click on a song
        list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
            if (!shouldDisplaySongs()) {
                // Clicked on a playlist element
                pView.currentPlaylist = pView.data.playlists!!.keys.elementAt(position)
                updateList()
            } else {
                // Clicked on a song
                onRandomFromSong(position)
            }
        }

        updateList()

        return view
    }

    lateinit var list: ListView

    lateinit var pView: MainActivity
    lateinit var displayedData : List<Song>

    /*fun onRandom(v: View) {
        val filteredData = displayedData.filter { currentPlaylist == null || it.playlist == currentPlaylist }
        val selectedMusics = filteredData.map { songToItem(data, it) }.shuffled().toMutableList()

        controllerFuture.get().setMediaItems(selectedMusics)

        controllerFuture.get().prepare()
        controllerFuture.get().play()
    }*/

    fun onRandomFromSong(position: Int) {

        val filteredData = displayedData.filter { pView.currentPlaylist == null || it.playlist == pView.currentPlaylist }
        val song = filteredData[position]

        val selectedMusics = filteredData.filter { it.playlist == song.playlist && it.path != song.path }.shuffled().map { songToItem(pView.data, it) }.toMutableList()
        selectedMusics.add(0, songToItem(pView.data, song))

        pView.controllerFuture.get().setMediaItems(selectedMusics)

        pView.controllerFuture.get().prepare()
        pView.controllerFuture.get().play()
    }

    fun songToItem(data: MusicData, song: Song): MediaItem {
        val albumPath = data.albums[song.album]?.path
        val builder = MediaMetadata.Builder()
        if (song.album != null) {
            builder.setArtist(song.artist)
            builder.setArtworkUri(Uri.parse("${requireContext().filesDir}/${pView.currUrl}icon/${albumPath}"))
        } else {
            builder.setArtist(null)
            builder.setArtworkUri(Uri.parse("https://${pView.currUrl}img/CD.png"))
        }
        return MediaItem.Builder()
            .setMediaId("${requireContext().filesDir}/${pView.currUrl}music/${song.path}")
            .setMediaMetadata(
                builder
                    .setTitle(song.name)
                    .setAlbumTitle(song.album)
                    .build()
            )
            .build()
    }

    // We display the songs if there is no playlist available or if we chose one already
    fun shouldDisplaySongs(): Boolean {
        return pView.data.playlists == null || pView.data.playlists!!.isEmpty() || pView.currentPlaylist != null;
    }

    fun updateList() {
        val pref = PreferenceManager.getDefaultSharedPreferences(requireContext())
        val sort = pref.getString("sort_mode", "DATE")
        val handler = Handler(Looper.getMainLooper())
        handler.post {
            val adapter =
                if (shouldDisplaySongs()) {
                    displayedData = pView.downloaded.filter { pView.currentPlaylist == null || it.playlist == pView.currentPlaylist }

                    if (sort == "RANDOM") displayedData = displayedData.shuffled()
                    else if (sort == "MUSICNAME") displayedData = displayedData.sortedBy { it.name }
                    else if (sort == "ARTISTNAME") displayedData = displayedData.sortedBy { it.artist }

                    SongAdapter(requireContext(), displayedData.map { ExtendedSong(it, pView.data.albums[it.album]) }, pView.currUrl!!)
                } else {
                    ArrayAdapter(requireContext(), android.R.layout.simple_list_item_1, pView.data.playlists!!.map { it.value.name })
                }
            list.adapter = adapter
        }
    }
}