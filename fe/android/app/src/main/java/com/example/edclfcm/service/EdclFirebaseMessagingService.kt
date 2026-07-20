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

        val hasData = remoteMessage.data.isNotEmpty()
        val hasNotification = remoteMessage.notification != null

        val title = remoteMessage.notification?.title 
                    ?: remoteMessage.data["title"] 
                    ?: "New Notification"
        
        val body = remoteMessage.notification?.body 
                   ?: remoteMessage.data["body"] 
                   ?: "You have a new message."
        
        if (hasData || hasNotification) {
            Log.d("FCM", "Message received. Showing notification.")
            showNotification(title, body, remoteMessage.data)
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

    private fun showNotification(title: String, messageBody: String, dataPayload: Map<String, String>? = null) {
        val channelId = "edcl_fcm_channel"
        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(channelId, "EDCL Notifications", NotificationManager.IMPORTANCE_HIGH)
            notificationManager.createNotificationChannel(channel)
        }

        val intent = android.content.Intent(this, com.example.edclfcm.MainActivity::class.java).apply {
            flags = android.content.Intent.FLAG_ACTIVITY_NEW_TASK or android.content.Intent.FLAG_ACTIVITY_CLEAR_TASK
            dataPayload?.get("notificationId")?.let { idStr ->
                putExtra("notificationId", idStr.toLongOrNull() ?: -1L)
            }
        }
        val pendingIntent = android.app.PendingIntent.getActivity(
            this, 
            0, 
            intent, 
            android.app.PendingIntent.FLAG_UPDATE_CURRENT or android.app.PendingIntent.FLAG_IMMUTABLE
        )

        val notificationBuilder = NotificationCompat.Builder(this, channelId)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentTitle(title)
            .setContentText(messageBody)
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setContentIntent(pendingIntent)

        notificationManager.notify(System.currentTimeMillis().toInt(), notificationBuilder.build())
    }
}
