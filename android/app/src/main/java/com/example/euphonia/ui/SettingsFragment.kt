package com.example.euphonia.ui

import android.app.AlertDialog
import android.content.Context.MODE_PRIVATE
import android.content.DialogInterface
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.widget.EditText
import android.widget.Toast
import androidx.lifecycle.lifecycleScope
import androidx.preference.Preference
import androidx.preference.PreferenceFragmentCompat
import com.example.euphonia.MainActivity
import com.example.euphonia.R
import com.example.euphonia.SetupActivity
import com.example.euphonia.data.TokenApiResp
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import okhttp3.Call
import okhttp3.Callback
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody
import okhttp3.Response
import java.io.File
import java.io.IOException

class SettingsFragment : PreferenceFragmentCompat() {

    var deleteConfirm = false

    override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?) {
        setPreferencesFromResource(R.xml.root_preferences, rootKey)

        val admin = findPreference<Preference>("admin")!!
        val sharedPref = requireContext().getSharedPreferences("settings", MODE_PRIVATE)
        var adminToken = sharedPref.getString("adminToken", null)
        admin.title = resources.getString(if (adminToken == null) {
            R.string.admin_login
        } else {
            R.string.admin_logoff
        })
        admin.setOnPreferenceClickListener {
            adminToken = sharedPref.getString("adminToken", null)
            if (adminToken != null) {
                with(sharedPref.edit()) {
                    putString("adminToken", null)
                    apply()
                }
                admin.title = resources.getString(R.string.admin_login)
            } else {
                val builder = AlertDialog.Builder(activity)
                builder.setTitle(R.string.admin_enter_password)
                builder.setCancelable(false)

                val et = EditText(activity)
                builder.setView(et)

                builder.setPositiveButton(android.R.string.ok) {
                    _, _ ->
                        val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
                        val index = sharedPref.getInt("currentServer", -1)
                        val okHttpClient = OkHttpClient()

                        val requestBody = RequestBody.create("application/json".toMediaType(), "\"${et.text}\"")
                        val request = Request.Builder()
                            .post(requestBody)
                            .url("https://${servers.elementAt(index)}api/auth/token")
                            .build()
                        okHttpClient.newCall(request).enqueue(object : Callback {
                            override fun onFailure(call: Call, e: IOException) {
                                lifecycleScope.launch(Dispatchers.Main) {
                                    Toast.makeText(activity, "Unexpected error", Toast.LENGTH_SHORT).show()
                                }
                            }

                            override fun onResponse(call: Call, response: Response) {
                                if (response.code != 200) {
                                    lifecycleScope.launch(Dispatchers.Main) {
                                        if (response.code == 401) {
                                            Toast.makeText(activity, resources.getString(R.string.admin_login_wrong_password), Toast.LENGTH_LONG).show()
                                        } else {
                                            Toast.makeText(activity, "Unexpected error ${response.code}", Toast.LENGTH_SHORT).show()
                                        }
                                    }
                                }
                                else
                                {
                                    lifecycleScope.launch(Dispatchers.Main) {
                                        val token = Gson().fromJson(response.body.string(), TokenApiResp::class.java).token
                                        Toast.makeText(activity, resources.getString(R.string.admin_login_success), Toast.LENGTH_LONG).show()
                                        admin.title = resources.getString(R.string.admin_logoff)

                                        with(sharedPref.edit()) {
                                            putString("adminToken", token)
                                            apply()
                                        }
                                    }
                                }
                            }
                        })
                }
                builder.setNegativeButton(android.R.string.cancel) {
                        _, _ -> { }
                }

                builder.show()
            }

            true
        }

        findPreference<Preference>("website")!!.setOnPreferenceClickListener {
            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
            val index = sharedPref.getInt("currentServer", -1)
            startActivity(
                Intent(
                    Intent.ACTION_VIEW,
                    Uri.parse("https://${servers.elementAt(index)}")
                )
            )
            true
        }

        findPreference<Preference>("github")!!.setOnPreferenceClickListener {
            startActivity(
                Intent(
                    Intent.ACTION_VIEW,
                    Uri.parse("https://github.com/Xwilarg/Euphonia")
                )
            )
            true
        }

        findPreference<Preference>("source_add")!!.setOnPreferenceClickListener {
            val builder = AlertDialog.Builder(activity)
            builder.setTitle(R.string.pref_source_set)

            val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!

            val options = servers.toMutableList()
            options.add(resources.getString(R.string.pref_source_add))

            val cleanOptions = options.toTypedArray()

            builder.setItems(cleanOptions) { _: DialogInterface, which: Int ->
                if (which == servers.count()) {
                    val intent = Intent(requireContext(), SetupActivity::class.java)
                    startActivity(intent)
                } else {
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

        findPreference<Preference>("source_remove")!!.setOnPreferenceClickListener {
            if (deleteConfirm) {
                val index = sharedPref.getInt("currentServer", -1)

                val servers = sharedPref.getStringSet("remoteServers", setOf<String>())!!
                val currUrl = servers.elementAt(index)
                File(requireContext().filesDir, currUrl).deleteRecursively()

                val intent = Intent(requireContext(), MainActivity::class.java)
                intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
                startActivity(intent)
                Runtime.getRuntime().exit(0)

            } else {
                findPreference<Preference>("source_remove")!!.title =
                    resources.getString(R.string.pref_deletion_confirm)

                deleteConfirm = true
            }
            true
        }
    }
}