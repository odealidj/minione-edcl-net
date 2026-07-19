package com.example.edclfcm.api

import com.example.edclfcm.util.TokenManager
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object ApiClient {
    // 10.0.2.2 is the alias for localhost from Android emulator
    // The native .NET backend (EDCL.Api) runs on port 5140
    private const val BASE_URL = "http://10.0.2.2:5140/"
    
    private lateinit var tokenManager: TokenManager

    fun init(manager: TokenManager) {
        tokenManager = manager
    }

    private val client by lazy {
        OkHttpClient.Builder()
            .addInterceptor { chain ->
                val original = chain.request()
                val token = tokenManager.getAccessToken()
                val requestBuilder = original.newBuilder()
                if (!token.isNullOrEmpty()) {
                    requestBuilder.header("Authorization", "Bearer $token")
                }
                val request = requestBuilder.build()
                chain.proceed(request)
            }
            .build()
    }

    private val retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    val service: ApiService by lazy {
        retrofit.create(ApiService::class.java)
    }
}
