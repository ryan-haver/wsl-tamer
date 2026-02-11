//! Tray menu building and event handling

use tauri::{
    AppHandle, Manager,
    menu::{Menu, MenuItem, Submenu, PredefinedMenuItem},
};
use crate::services::WslService;
use crate::commands::get_profile_manager;

/// Build the tray context menu
pub fn build_tray_menu(app: &AppHandle) -> Result<Menu<tauri::Wry>, tauri::Error> {
    let is_running = WslService::is_wsl_running();
    
    // Status indicator
    let status_text = if is_running { "● Running" } else { "○ Stopped" };
    let status = MenuItem::with_id(app, "status", status_text, false, None::<&str>)?;
    
    let separator1 = PredefinedMenuItem::separator(app)?;
    
    // Quick actions
    let launch_wsl = MenuItem::with_id(app, "launch_wsl", "Launch WSL", true, None::<&str>)?;
    let start_background = MenuItem::with_id(app, "start_background", "Start in Background", true, None::<&str>)?;
    
    let separator2 = PredefinedMenuItem::separator(app)?;
    
    // Profile submenu - build items inline
    let profiles = get_profile_manager().get_profiles();
    let current_profile = get_profile_manager().get_current_profile();
    let current_id = current_profile.map(|p| p.id);
    
    // Build profile submenu
    let profiles_submenu = {
        let submenu = Submenu::with_id(app, "profiles", "Profiles", true)?;
        for profile in &profiles {
            let prefix = if current_id.as_ref() == Some(&profile.id) { "✓ " } else { "  " };
            let item = MenuItem::with_id(
                app, 
                &format!("profile_{}", profile.id),
                &format!("{}{}", prefix, profile.name),
                true,
                None::<&str>
            )?;
            submenu.append(&item)?;
        }
        submenu
    };
    
    let separator3 = PredefinedMenuItem::separator(app)?;
    
    // WSL actions
    let shutdown = MenuItem::with_id(app, "shutdown", "Shutdown WSL", true, None::<&str>)?;
    let reclaim = MenuItem::with_id(app, "reclaim", "Reclaim Memory", true, None::<&str>)?;
    
    let separator4 = PredefinedMenuItem::separator(app)?;
    
    // Settings and exit
    let settings = MenuItem::with_id(app, "settings", "Settings...", true, None::<&str>)?;
    let exit = MenuItem::with_id(app, "exit", "Exit", true, None::<&str>)?;
    
    Menu::with_items(app, &[
        &status,
        &separator1,
        &launch_wsl,
        &start_background,
        &separator2,
        &profiles_submenu,
        &separator3,
        &shutdown,
        &reclaim,
        &separator4,
        &settings,
        &exit,
    ])
}

/// Handle tray menu events
pub fn handle_tray_menu_event(app: &AppHandle, id: &str) {
    match id {
        "launch_wsl" => {
            // Launch default distro in terminal
            if let Ok(distros) = WslService::get_distributions() {
                if let Some(default) = distros.iter().find(|d| d.is_default) {
                    let _ = WslService::start_distribution(&default.name);
                }
            }
        }
        "start_background" => {
            if let Ok(distros) = WslService::get_distributions() {
                if let Some(default) = distros.iter().find(|d| d.is_default) {
                    let _ = WslService::start_distribution_background(&default.name);
                }
            }
        }
        "shutdown" => {
            let _ = WslService::shutdown_all();
        }
        "reclaim" => {
            let _ = WslService::reclaim_memory();
        }
        "settings" => {
            // Show settings window
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.show();
                let _ = window.set_focus();
            }
        }
        "exit" => {
            app.exit(0);
        }
        id if id.starts_with("profile_") => {
            let profile_id = id.trim_start_matches("profile_");
            if let Some(profile) = get_profile_manager().get_profile(profile_id) {
                let _ = WslService::apply_profile(&profile);
                let _ = get_profile_manager().set_current_profile(profile_id);
                // Rebuild menu to update checkmarks
                if let Some(tray) = app.tray_by_id("main") {
                    if let Ok(menu) = build_tray_menu(app) {
                        let _ = tray.set_menu(Some(menu));
                    }
                }
            }
        }
        _ => {}
    }
}
