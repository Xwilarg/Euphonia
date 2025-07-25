package com.example.euphonia.ui

import android.app.AlertDialog
import android.content.Context.MODE_PRIVATE
import android.net.Uri
import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.os.Handler
import android.os.Looper
import android.text.Editable
import android.text.TextWatcher
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.EditText
import android.widget.ListView
import android.widget.TextView
import android.widget.Toast
import androidx.activity.OnBackPressedCallback
import androidx.lifecycle.lifecycleScope
import androidx.media3.common.MediaItem
import androidx.media3.common.MediaMetadata
import androidx.preference.PreferenceManager
import com.example.euphonia.R
import com.example.euphonia.SongAdapter
import com.example.euphonia.data.ExtendedSong
import com.example.euphonia.data.MusicData
import com.example.euphonia.data.Song
import com.example.euphonia.MainActivity
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.io.File
import java.nio.file.Files
import java.nio.file.Path

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

        searchBar = view.findViewById<EditText>(R.id.searchBar)
        searchBar.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(
                s: CharSequence?,
                start: Int,
                count: Int,
                after: Int
            ) { }

            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}

            override fun afterTextChanged(s: Editable?) {
                searchFilter = s.toString().uppercase()
                updateList()
            }
        })

        requireActivity()
            .onBackPressedDispatcher
            .addCallback(viewLifecycleOwner, object : OnBackPressedCallback(true) {
                override fun handleOnBackPressed() {
                    if (pView.currentPlaylist != null) {
                        pView.currentPlaylist = null
                        updateList()
                    } else {
                        requireActivity().onBackPressedDispatcher.onBackPressed()
                    }
                }
            })

        list = view.findViewById(R.id.musicData)
        list.onItemLongClickListener = AdapterView.OnItemLongClickListener { parent, v, position, id ->

            val ctx = requireContext()

            val sharedPref = ctx.getSharedPreferences("settings", MODE_PRIVATE)
            val index = sharedPref.getInt("currentServer", -1)

            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
            val currUrl = servers.elementAt(index)

            val song = displayedData[position]

            val target = File(ctx.filesDir, "${currUrl}music/${song.path}")

            if (target.exists()) {
                val builder = AlertDialog.Builder(ctx)
                builder.setTitle(R.string.invalid_song_title)
                builder.setCancelable(true)

                val et = TextView(ctx)
                et.setText(ctx.resources.getString(R.string.invalid_song_text, song.name))
                builder.setView(et)

                builder.setPositiveButton(android.R.string.ok) { _, _ ->

                    lifecycleScope.launch(Dispatchers.Main) {
                        Files.delete(target.toPath())
                        Toast.makeText(ctx, ctx.resources.getString(R.string.song_redownload_info), Toast.LENGTH_LONG).show()
                    }
                }
                builder.setNegativeButton(android.R.string.cancel) {
                        _, _ -> { }
                }
                builder.show()
            } else {
                Toast.makeText(ctx, ctx.resources.getString(R.string.song_already_deleted), Toast.LENGTH_LONG).show()
            }

            true
        }
        // Callback when we click on a song
        list.onItemClickListener = AdapterView.OnItemClickListener { parent, v, position, id ->
            if (!shouldDisplaySongs()) {
                var pIndex = position
                if (pView.metadata.showAllPlaylist == true) {
                    pIndex -= 1
                }

                // Clicked on a playlist element
                pView.currentPlaylist =
                    if (pIndex == -1) // Show all
                        "All"
                    else
                        pView.data.playlists!!.keys.elementAt(pIndex)
                updateList()
            } else {
                // Clicked on a song
                onRandomFromSong(position)
            }
        }

        view.findViewById<Button>(R.id.random).setOnClickListener { onRandom() }

        updateList()

        return view
    }

    lateinit var list: ListView

    lateinit var pView: MainActivity
    lateinit var displayedData : List<Song>
    lateinit var searchBar: EditText
    var searchFilter: String = ""

    fun onRandom() {
        val pref = PreferenceManager.getDefaultSharedPreferences(requireContext())
        val sort = pref.getString("play_mode", "RANDOM")

        val filteredData = getCurrentMusics()

        var selectedMusics = filteredData.map { songToItem(pView.data, it) }
        if (sort == "RANDOM") {
            selectedMusics = selectedMusics.shuffled()
        }

        pView.controllerFuture!!.get().setMediaItems(selectedMusics)

        pView.controllerFuture!!.get().prepare()
        pView.controllerFuture!!.get().play()
    }

    fun onRandomFromSong(position: Int) {
        val pref = PreferenceManager.getDefaultSharedPreferences(requireContext())
        val sort = pref.getString("play_mode", "RANDOM")

        val filteredData = getCurrentMusics()
        val song = displayedData[position]

        var selectedMusics: MutableList<MediaItem>
        if (sort == "RANDOM") {
            selectedMusics = filteredData.filter { it.path != song.path }.shuffled().map { songToItem(pView.data, it) }.toMutableList()
            selectedMusics.add(0, songToItem(pView.data, song))
        } else { // Play in order
            selectedMusics = filteredData.map { songToItem(pView.data, it) }.toMutableList()
            selectedMusics.drop(position)
        }

        pView.controllerFuture!!.get().setMediaItems(selectedMusics)

        pView.controllerFuture!!.get().prepare()
        pView.controllerFuture!!.get().play()
    }

    fun songToItem(data: MusicData, song: Song): MediaItem {
        val albumPath = data.albums[song.album]?.path
        val builder = MediaMetadata.Builder()
        if (song.album != null) {
            builder.setArtworkUri(Uri.parse("${requireContext().filesDir}/${pView.currUrl}icon/${albumPath}"))
            builder.setAlbumTitle(data.albums[song.album]?.name)
        } else {
            builder.setArtworkUri(Uri.parse("https://${pView.currUrl}img/CD.png"))
        }
        return MediaItem.Builder()
            .setMediaId("${requireContext().filesDir}/${pView.currUrl}music/${song.path}")
            .setMediaMetadata(
                builder
                    .setTitle(song.name)
                    .setDisplayTitle(if (song.key != null) {
                        song.key
                    } else {
                        "${song.name}_${song.artist ?: ""}_${song.type ?: ""}"
                    })
                    .setArtist(song.artist)
                    .setAlbumTitle(song.album)
                    .build()
            )
            .build()
    }

    // We display the songs if there is no playlist available or if we chose one already
    fun shouldDisplaySongs(): Boolean {
        return pView.data.playlists == null || pView.data.playlists!!.isEmpty() || pView.currentPlaylist != null;
    }

    fun getCurrentMusics(): List<Song> {
        val tmp = mutableListOf<Song>()
        tmp.addAll(pView.downloaded)
        return tmp.filter { !it.isArchived && (pView.currentPlaylist == null || pView.currentPlaylist == "All" || it.playlists.contains(pView.currentPlaylist)) }
    }

    fun updateList() {
        val pref = PreferenceManager.getDefaultSharedPreferences(requireContext())
        val sort = pref.getString("sort_mode", "DATE")

        val handler = Handler(Looper.getMainLooper())
        handler.post {
            val adapter =
                if (shouldDisplaySongs()) {
                    searchBar.visibility = View.VISIBLE

                    displayedData = getCurrentMusics()

                    if (sort == "RANDOM") displayedData = displayedData.shuffled()
                    else if (sort == "MUSICNAME") displayedData = displayedData.sortedBy { it.name }
                    else if (sort == "ARTISTNAME") displayedData = displayedData.sortedBy { it.artist }

                    displayedData = displayedData.filter { it.name.uppercase().contains(searchFilter) || (it.artist != null && it.artist.uppercase().contains(searchFilter)) }

                    SongAdapter(requireContext(), displayedData.map { ExtendedSong(it, pView.data.albums[it.album]) }, pView.currUrl!!)
                } else {
                    searchBar.visibility = View.INVISIBLE
                    val data = mutableListOf<String>()
                    if (pView.metadata.showAllPlaylist == true) {
                        data.add("All")
                    }
                    data.addAll(pView.data.playlists!!.map { it.value.name }.toMutableList())
                    ArrayAdapter(requireContext(), android.R.layout.simple_list_item_1, data)
                }
            list.adapter = adapter
        }
    }
}