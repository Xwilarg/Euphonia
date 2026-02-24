package eu.zirk.euphonia

import android.app.AlertDialog
import android.app.Dialog
import android.content.Context
import android.content.Context.MODE_PRIVATE
import android.content.DialogInterface
import android.net.Uri
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.media3.common.Label
import eu.zirk.euphonia.data.ExtendedSong
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
        if (currentSong.imagePath == null) {
            image.setImageResource(R.drawable.album)
        } else {
            image.setImageURI(Uri.fromFile(File("${mContext.filesDir}/${currUrl}icon/${currentSong.imagePath}")))
        }

        val name = listItem.findViewById<View>(R.id.textView_name) as TextView
        name.setText(currentSong.song.name)

        val release = listItem.findViewById<View>(R.id.textView_artist) as TextView
        release.setText(currentSong.song.artist)

        return listItem
    }
}
