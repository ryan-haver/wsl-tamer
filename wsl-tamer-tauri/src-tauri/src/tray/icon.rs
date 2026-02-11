//! Dynamic tray icon generation

use tauri::image::Image;

/// RGBA color structure
#[derive(Clone, Copy)]
pub struct Rgba {
    pub r: u8,
    pub g: u8,
    pub b: u8,
    pub a: u8,
}

/// Icon colors
pub const COLOR_RUNNING: Rgba = Rgba { r: 0, g: 200, b: 0, a: 255 };   // Green
pub const COLOR_STOPPED: Rgba = Rgba { r: 150, g: 150, b: 150, a: 255 }; // Gray

/// Generate a simple colored status icon
pub fn generate_status_icon(is_running: bool) -> Image<'static> {
    let size = 32u32;
    let mut pixels = vec![0u8; (size * size * 4) as usize];
    
    let color = if is_running { COLOR_RUNNING } else { COLOR_STOPPED };
    
    // Create a simple circle icon
    let center = size as f32 / 2.0;
    let radius = (size as f32 / 2.0) - 2.0;
    
    for y in 0..size {
        for x in 0..size {
            let dx = x as f32 - center;
            let dy = y as f32 - center;
            let distance = (dx * dx + dy * dy).sqrt();
            
            let idx = ((y * size + x) * 4) as usize;
            
            if distance <= radius {
                // Inside circle
                pixels[idx] = color.r;
                pixels[idx + 1] = color.g;
                pixels[idx + 2] = color.b;
                pixels[idx + 3] = color.a;
            } else if distance <= radius + 1.0 {
                // Anti-aliased edge
                let alpha = ((radius + 1.0 - distance) * 255.0) as u8;
                pixels[idx] = color.r;
                pixels[idx + 1] = color.g;
                pixels[idx + 2] = color.b;
                pixels[idx + 3] = alpha;
            } else {
                // Outside - transparent
                pixels[idx] = 0;
                pixels[idx + 1] = 0;
                pixels[idx + 2] = 0;
                pixels[idx + 3] = 0;
            }
        }
    }
    
    Image::new_owned(pixels, size, size)
}

/// Generate terminal-style icon with ">_" prompt
#[allow(dead_code)]
pub fn generate_terminal_icon(is_running: bool) -> Image<'static> {
    // For now, use simple colored circle
    // TODO: Render ">_" text on icon
    generate_status_icon(is_running)
}
