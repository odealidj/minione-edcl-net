package com.example.edclfcm.util

import android.content.Context
import android.content.SharedPreferences

class TokenManager(context: Context) {
    private val prefs: SharedPreferences = context.getSharedPreferences("edcl_prefs", Context.MODE_PRIVATE)

    fun saveAccessToken(token: String) {
        prefs.edit().putString("access_token", token).apply()
    }

    fun getAccessToken(): String? {
        return prefs.getString("access_token", null)
    }

    fun saveFcmToken(token: String) {
        prefs.edit().putString("fcm_token", token).apply()
    }

    fun getFcmToken(): String? {
        return prefs.getString("fcm_token", null)
    }
    
    fun clear() {
        prefs.edit().clear().apply()
    }
}
