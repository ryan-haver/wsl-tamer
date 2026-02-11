//! WSL Tamer - Advanced WSL2 Distribution Manager
//!
//! A Tauri 2.0 application for managing WSL2 distributions, profiles,
//! hardware passthrough, and automation.

pub mod commands;
pub mod models;
pub mod services;
pub mod tray;
pub mod utils;

use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::time::Duration;
use tauri::{
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    Manager, WindowEvent,
};

use crate::commands::*;
use crate::services::WslService;
use crate::tray::{build_tray_menu, generate_status_icon, handle_tray_menu_event};

/// Application entry point
#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        // Initialize plugins
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            // Focus existing window on second instance
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.show();
                let _ = window.set_focus();
            }
        }))
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_store::Builder::new().build())
        .plugin(tauri_plugin_autostart::init(
            tauri_plugin_autostart::MacosLauncher::LaunchAgent,
            None,
        ))
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_process::init())
        // Register all IPC command handlers
        .invoke_handler(tauri::generate_handler![
            // WSL commands
            get_distributions,
            is_wsl_running,
            get_wsl_status,
            start_distribution,
            start_distribution_background,
            stop_distribution,
            shutdown_wsl,
            kill_all_wsl,
            set_default_distribution,
            reclaim_memory,
            export_distribution,
            import_distribution,
            clone_distribution,
            move_distribution,
            unregister_distribution,
            get_online_distributions,
            install_distribution,
            open_wsl_explorer,
            read_wslconfig,
            write_wslconfig,
            apply_wsl_profile,
            read_distro_config,
            write_distro_config,
            // Profile commands
            get_profiles,
            get_profile,
            get_current_profile,
            save_profile,
            delete_profile,
            set_default_profile,
            apply_profile,
            get_automation_rules,
            save_automation_rule,
            delete_automation_rule,
            toggle_automation_rule,
            get_app_config,
            load_app_config,
            // Hardware commands
            is_usbipd_installed,
            get_usb_devices,
            attach_usb_device,
            detach_usb_device,
            get_physical_disks,
            mount_disk,
            unmount_disk,
            mount_folder,
            unmount_folder,
            // Config commands
            get_start_with_windows,
            set_start_with_windows,
            get_theme,
            set_theme,
            get_wslconfig_typed,
            save_wslconfig_typed,
            // Monitoring commands
            get_system_metrics,
            get_distro_metrics,
            // Automation commands
            services::automation_engine::get_system_state,
            services::automation_engine::evaluate_automation_rule,
            services::automation_engine::get_power_state,
            services::automation_engine::get_running_processes,
        ])
        // Setup application
        .setup(|app| {
            // Initialize profile manager with defaults
            let _ = get_profile_manager();

            // Build initial tray menu
            let menu = build_tray_menu(app.handle())?;

            // Create system tray
            let is_running = WslService::is_wsl_running();
            let icon = generate_status_icon(is_running);

            let _tray = TrayIconBuilder::with_id("main")
                .icon(icon)
                .tooltip("WSL Tamer")
                .menu(&menu)
                .on_menu_event(|app, event| {
                    handle_tray_menu_event(app, event.id().as_ref());
                })
                .on_tray_icon_event(|tray, event| {
                    match event {
                        TrayIconEvent::Click {
                            button: MouseButton::Left,
                            button_state: MouseButtonState::Up,
                            ..
                        } => {
                            // Show settings window on left click
                            let app = tray.app_handle();
                            if let Some(window) = app.get_webview_window("main") {
                                let _ = window.show();
                                let _ = window.set_focus();
                            }
                        }
                        _ => {}
                    }
                })
                .build(app)?;

            // Hide main window initially (tray-only mode)
            if let Some(window) = app.get_webview_window("main") {
                // Hide to tray on close instead of quitting
                let app_handle = app.handle().clone();
                window.on_window_event(move |event| {
                    if let WindowEvent::CloseRequested { api, .. } = event {
                        // Hide instead of close
                        if let Some(window) = app_handle.get_webview_window("main") {
                            let _ = window.hide();
                        }
                        api.prevent_close();
                    }
                });
            }

            // Start status polling timer with graceful shutdown
            let app_handle = app.handle().clone();
            let shutdown_flag = Arc::new(AtomicBool::new(false));
            let shutdown_for_thread = shutdown_flag.clone();

            // Spawn polling thread
            std::thread::spawn(move || {
                let mut last_running = WslService::is_wsl_running();

                while !shutdown_for_thread.load(Ordering::Relaxed) {
                    // Use shorter sleep intervals for responsive shutdown
                    for _ in 0..30 {
                        if shutdown_for_thread.load(Ordering::Relaxed) {
                            return;
                        }
                        std::thread::sleep(Duration::from_secs(1));
                    }

                    let is_running = WslService::is_wsl_running();
                    if is_running != last_running {
                        last_running = is_running;

                        // Update tray icon only when status changes
                        if let Some(tray) = app_handle.tray_by_id("main") {
                            let icon = generate_status_icon(is_running);
                            let _ = tray.set_icon(Some(icon));

                            // Rebuild menu to update status text
                            if let Ok(menu) = build_tray_menu(&app_handle) {
                                let _ = tray.set_menu(Some(menu));
                            }
                        }
                    }
                }
            });

            // Store shutdown flag for cleanup on app exit
            app.manage(shutdown_flag);

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running WSL Tamer");
}
