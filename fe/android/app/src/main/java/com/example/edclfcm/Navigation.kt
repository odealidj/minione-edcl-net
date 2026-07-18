package com.example.edclfcm

import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation3.runtime.entryProvider
import androidx.navigation3.runtime.rememberNavBackStack
import androidx.navigation3.ui.NavDisplay
import androidx.compose.ui.platform.LocalContext
import androidx.navigation3.runtime.NavKey
import com.example.edclfcm.ui.HomeScreen
import com.example.edclfcm.ui.LoginScreen
import com.example.edclfcm.util.TokenManager
import kotlinx.serialization.Serializable

sealed interface Screen : NavKey
@Serializable data object Login : Screen
@Serializable data object Home : Screen

@Composable
fun MainNavigation() {
  val context = LocalContext.current
  val tokenManager = TokenManager(context)
  
  val initialScreen: Screen = if (tokenManager.getAccessToken().isNullOrEmpty()) Login else Home
  
  val backStack = rememberNavBackStack(initialScreen)

  NavDisplay(
    backStack = backStack,
    onBack = { backStack.removeLastOrNull() },
    entryProvider =
      entryProvider {
        entry<Login> {
          LoginScreen(onLoginSuccess = { 
            backStack.clear()
            backStack.add(Home) 
          })
        }
        entry<Home> {
          HomeScreen(onLogout = { 
            backStack.clear()
            backStack.add(Login) 
          })
        }
      },
  )
}
