package com.example.euphonia

import android.content.Context
import android.net.Uri
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.ImageView
import android.widget.TextView
import com.example.euphonia.data.ExtendedSong
import java.io.File

class SongAdapter(private val mContext: Context, list: List<ExtendedSong>, url: String) :
    ArrayAdapter<ExtendedSong>(
        mContext, 0, list
    ) {
    private val songList: List<ExtendedSong> = list
    private val currUrl: String = url

    override fun getView(position: Int, convertView: View?, parent: ViewGroup): View {
        var listItem = convertView
        if (listItem == null) listItem =
            LayoutInflater.from(mContext).inflate(R.layout.music_list, parent, false)

        val currentSong = songList[position]

        val image = listItem!!.findViewById<View>(R.id.imageView_poster) as ImageView
        if (currentSong.album == null) {
            image.setImageResource(R.drawable.album)
        } else {
            image.setImageURI(Uri.fromFile(File("${mContext.filesDir}/${currUrl}icon/${currentSong.album?.path}")))
        }

        val name = listItem.findViewById<View>(R.id.textView_name) as TextView
        name.setText(currentSong.song.name)

        val release = listItem.findViewById<View>(R.id.textView_artist) as TextView
        release.setText(currentSong.song.artist)

        return listItem
    }
}
