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

data class ApiError(
    val field: String,
    val code: String,
    val message: String
)

data class ApiResponse<T>(
    val code: Int,
    val message: String,
    val data: T?,
    val status: String,
    val errors: List<ApiError>? = null
)

data class FcmTokenRequest(
    val fcmToken: String
)

data class ChangePinRequest(
    val phoneNumber: String,
    val oldPin: String,
    val newPin: String,
    val deviceName: String = "EdclFcmSimulator"
)
