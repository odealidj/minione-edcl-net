package com.example.edclfcm.service

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import com.example.edclfcm.api.ApiClient
import com.example.edclfcm.api.FcmTokenRequest
import com.example.edclfcm.util.TokenManager
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class EdclFirebaseMessagingService : FirebaseMessagingService() {

    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        super.onMessageReceived(remoteMessage)
        Log.d("FCM", "From: ${remoteMessage.from}")

        // Check if message contains a data payload.
        if (remoteMessage.data.isNotEmpty()) {
            Log.d("FCM", "Message data payload: ${remoteMessage.data}")
            val title = remoteMessage.data["title"] ?: "New Notification"
            val body = remoteMessage.data["body"] ?: "You have a new message."
            showNotification(title, body)
        }

        // Check if message contains a notification payload.
        remoteMessage.notification?.let {
            Log.d("FCM", "Message Notification Body: ${it.body}")
            showNotification(it.title ?: "Notification", it.body ?: "")
        }
    }

    override fun onNewToken(token: String) {
        Log.d("FCM", "Refreshed token: $token")
        val tokenManager = TokenManager(applicationContext)
        tokenManager.saveFcmToken(token)

        // If user is logged in, send token to backend
        if (!tokenManager.getAccessToken().isNullOrEmpty()) {
            CoroutineScope(Dispatchers.IO).launch {
                try {
                    ApiClient.service.updateFcmToken(FcmTokenRequest(token))
                } catch (e: Exception) {
                    Log.e("FCM", "Failed to update token on server", e)
                }
            }
        }
    }

    private fun showNotification(title: String, messageBody: String) {
        val channelId = "edcl_fcm_channel"
        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(channelId, "EDCL Notifications", NotificationManager.IMPORTANCE_HIGH)
            notificationManager.createNotificationChannel(channel)
        }

        val notificationBuilder = NotificationCompat.Builder(this, channelId)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentTitle(title)
            .setContentText(messageBody)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)

        notificationManager.notify(System.currentTimeMillis().toInt(), notificationBuilder.build())
    }
}
