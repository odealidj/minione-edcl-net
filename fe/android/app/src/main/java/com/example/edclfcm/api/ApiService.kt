package com.example.edclfcm.api

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST
import retrofit2.http.PUT

interface ApiService {
    @POST("api/v1/mobile/auth/drivers/login")
    suspend fun login(@Body request: LoginRequest): Response<ApiResponse<LoginResponse>>

    @PUT("api/v1/mobile/auth/drivers/fcm-token")
    suspend fun updateFcmToken(@Body request: FcmTokenRequest): Response<ApiResponse<Any>>

    @POST("api/v1/mobile/auth/drivers/change-pin")
    suspend fun changePin(@Body request: ChangePinRequest): Response<ApiResponse<LoginResponse>>
}
