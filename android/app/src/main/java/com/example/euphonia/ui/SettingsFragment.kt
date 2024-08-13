package com.example.euphonia.ui

import android.R.attr.onClick
import android.app.AlertDialog
import android.content.Context.MODE_PRIVATE
import android.content.DialogInterface
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import androidx.preference.Preference
import androidx.preference.PreferenceFragmentCompat
import com.example.euphonia.MainActivity
import com.example.euphonia.R
import com.example.euphonia.SetupActivity

class SettingsFragment : PreferenceFragmentCompat() {

    override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?) {
        setPreferencesFromResource(R.xml.root_preferences, rootKey)


        findPreference<Preference>("website")!!.setOnPreferenceClickListener {
            val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
            val index = sharedPref.getInt("currentServer", -1)
            startActivity(Intent(Intent.ACTION_VIEW, Uri.parse("https://${servers.elementAt(index)}")))
            true
        }

        findPreference<Preference>("source")!!.setOnPreferenceClickListener {
            val builder = AlertDialog.Builder(activity)
            builder.setTitle(R.string.pref_source_set)

            val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!

            val options = servers.toMutableList()
            options.add(resources.getString(R.string.pref_source_add))

            val cleanOptions = options.toTypedArray()

            builder.setItems(cleanOptions) { _: DialogInterface, which: Int ->
                if (which == servers.count())
                {
                    val intent = Intent(requireContext(), SetupActivity::class.java)
                    startActivity(intent)
                }
                else
                {
                    with(sharedPref.edit()) {
                        putInt("currentServer", which)
                        apply()
                    }
                    (requireActivity() as MainActivity).loadData()
                }
            }
            builder.show()

            true
        }
    }
}