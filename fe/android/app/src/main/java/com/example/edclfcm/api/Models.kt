package com.example.edclfcm.api

data class LoginRequest(
    val phoneNumber: String,
    val pin: String,
    val deviceName: String = "EdclFcmSimulator"
)

data class LoginResponse(
    val driverId: Long,
    val name: String,
    val accessToken: String,
    val refreshToken: String
)

data class ApiResponse<T>(
    val code: Int,
    val message: String,
    val data: T?,
    val isSuccess: Boolean
)

data class FcmTokenRequest(
    val fcmToken: String
)
