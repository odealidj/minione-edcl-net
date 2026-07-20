package com.example.edclfcm

import android.os.Bundle
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.example.edclfcm.theme.EdclFcmTheme
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {
  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)

    enableEdgeToEdge()
    com.example.edclfcm.api.ApiClient.init(com.example.edclfcm.util.TokenManager(this))

    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
        val channelId = "edcl_fcm_channel"
        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val channel = NotificationChannel(channelId, "EDCL Notifications", NotificationManager.IMPORTANCE_HIGH)
        notificationManager.createNotificationChannel(channel)
    }

    handleNotificationIntent(intent)

    setContent {
      EdclFcmTheme { Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) { MainNavigation() } }
    }
  }

  override fun onNewIntent(intent: android.content.Intent) {
    super.onNewIntent(intent)
    handleNotificationIntent(intent)
  }

  private fun handleNotificationIntent(intent: android.content.Intent?) {
    val extraVal = intent?.extras?.get("notificationId")
    val notificationId = when (extraVal) {
        is Long -> extraVal
        is String -> extraVal.toLongOrNull() ?: -1L
        else -> -1L
    }
    
    android.util.Log.d("MainActivity", "handleNotificationIntent: extraVal=$extraVal, parsed notificationId=$notificationId")
    
    if (notificationId != -1L) {
        kotlinx.coroutines.CoroutineScope(kotlinx.coroutines.Dispatchers.IO).launch {
            try {
                val response = com.example.edclfcm.api.ApiClient.service.markAsRead(notificationId)
                android.util.Log.d("MainActivity", "markAsRead response: isSuccessful=${response.isSuccessful}, code=${response.code()}")
            } catch (e: Exception) {
                android.util.Log.e("MainActivity", "Failed to mark as read", e)
            }
        }
    }
  }
}
