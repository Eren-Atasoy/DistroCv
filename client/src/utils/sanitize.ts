/**
 * XSS Protection Utilities
 * 
 * This module provides functions to sanitize user input and prevent XSS attacks.
 * React automatically escapes content in JSX, but we need additional protection for:
 * - HTML content from APIs
 * - User-generated content
 * - Dynamic HTML insertion
 */

import DOMPurify from 'dompurify';

/**
 * Sanitize HTML content to prevent XSS attacks
 * Uses DOMPurify to remove malicious code while preserving safe HTML
 * 
 * @param dirty - Potentially unsafe HTML string
 * @returns Sanitized HTML string safe for rendering
 */
export function sanitizeHtml(dirty: string): string {
    return DOMPurify.sanitize(dirty, {
        ALLOWED_TAGS: [
            'p', 'br', 'strong', 'em', 'u', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
            'ul', 'ol', 'li', 'a', 'span', 'div', 'blockquote', 'code', 'pre'
        ],
        ALLOWED_ATTR: ['href', 'target', 'rel', 'class'],
        ALLOW_DATA_ATTR: false,
        ALLOWED_URI_REGEXP: /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i,
    });
}

/**
 * Sanitize plain text by escaping HTML entities
 * Use this for user input that should be displayed as plain text
 * 
 * @param text - User input text
 * @returns Escaped text safe for display
 */
export function escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Sanitize URL to prevent javascript: and data: URI attacks
 * 
 * @param url - URL to sanitize
 * @returns Safe URL or empty string if malicious
 */
export function sanitizeUrl(url: string): string {
    const trimmed = url.trim().toLowerCase();

    // Block dangerous protocols
    if (
        trimmed.startsWith('javascript:') ||
        trimmed.startsWith('data:') ||
        trimmed.startsWith('vbscript:') ||
        trimmed.startsWith('file:')
    ) {
        return '';
    }

    // Allow http, https, mailto, tel
    if (
        trimmed.startsWith('http://') ||
        trimmed.startsWith('https://') ||
        trimmed.startsWith('mailto:') ||
        trimmed.startsWith('tel:') ||
        trimmed.startsWith('/')
    ) {
        return url;
    }

    // Default to empty for suspicious URLs
    return '';
}

/**
 * Sanitize filename to prevent path traversal attacks
 * 
 * @param filename - Filename to sanitize
 * @returns Safe filename
 */
export function sanitizeFilename(filename: string): string {
    // Remove path separators and special characters
    return filename
        .replace(/[\/\\]/g, '')
        .replace(/\.\./g, '')
        .replace(/[<>:"|?*]/g, '')
        .trim();
}

/**
 * Validate and sanitize email address
 * 
 * @param email - Email address to validate
 * @returns Sanitized email or empty string if invalid
 */
export function sanitizeEmail(email: string): string {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const trimmed = email.trim().toLowerCase();

    if (emailRegex.test(trimmed)) {
        return trimmed;
    }

    return '';
}

/**
 * Sanitize JSON string to prevent injection
 * 
 * @param jsonString - JSON string to parse
 * @returns Parsed and sanitized object or null if invalid
 */
export function sanitizeJson<T = any>(jsonString: string): T | null {
    try {
        const parsed = JSON.parse(jsonString);
        // Re-stringify and parse to remove any functions or dangerous content
        return JSON.parse(JSON.stringify(parsed));
    } catch {
        return null;
    }
}

/**
 * Create safe HTML attributes object
 * Filters out event handlers and dangerous attributes
 * 
 * @param attrs - Attributes object
 * @returns Safe attributes object
 */
export function sanitizeAttributes(attrs: Record<string, any>): Record<string, any> {
    const safe: Record<string, any> = {};
    const dangerousAttrs = ['onerror', 'onload', 'onclick', 'onmouseover', 'onfocus', 'onblur'];

    for (const [key, value] of Object.entries(attrs)) {
        // Skip event handlers
        if (key.toLowerCase().startsWith('on')) {
            continue;
        }

        // Skip dangerous attributes
        if (dangerousAttrs.includes(key.toLowerCase())) {
            continue;
        }

        // Sanitize href and src attributes
        if (key === 'href' || key === 'src') {
            safe[key] = sanitizeUrl(String(value));
        } else {
            safe[key] = value;
        }
    }

    return safe;
}

/**
 * Content Security Policy helper
 * Use this to set CSP headers in API responses
 */
export const CSP_DIRECTIVES = {
    'default-src': ["'self'"],
    'script-src': ["'self'", "'unsafe-inline'"], // Consider removing unsafe-inline in production
    'style-src': ["'self'", "'unsafe-inline'", 'https://fonts.googleapis.com'],
    'img-src': ["'self'", 'data:', 'https:'],
    'font-src': ["'self'", 'https://fonts.gstatic.com'],
    'connect-src': ["'self'", 'https://api.gemini.google.com'],
    'frame-ancestors': ["'none'"],
    'base-uri': ["'self'"],
    'form-action': ["'self'"],
};

/**
 * Generate CSP header string
 */
export function generateCSPHeader(): string {
    return Object.entries(CSP_DIRECTIVES)
        .map(([directive, sources]) => `${directive} ${sources.join(' ')}`)
        .join('; ');
}
