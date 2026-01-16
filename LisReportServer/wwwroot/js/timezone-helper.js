/**
 * 前端时区处理助手
 * 提供统一的时区转换和展示功能
 */

class TimezoneHelper {
    constructor() {
        this.clientTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        this.timezoneOffset = this.getTimezoneOffset();
    }

    /**
     * 获取客户端时区偏移
     */
    getTimezoneOffset() {
        const now = new Date();
        const offsetMinutes = -now.getTimezoneOffset();
        const sign = offsetMinutes >= 0 ? '+' : '-';
        const absOffset = Math.abs(offsetMinutes);
        const hours = Math.floor(absOffset / 60).toString().padStart(2, '0');
        const minutes = (absOffset % 60).toString().padStart(2, '0');
        return `${sign}${hours}:${minutes}`;
    }

    /**
     * 将UTC时间字符串转换为本地时间
     */
    convertUtcToLocal(utcTimeString) {
        try {
            // 确保输入的是UTC时间格式
            let utcDate;
            if (typeof utcTimeString === 'string') {
                // 如果字符串末尾没有Z，添加Z表示UTC时间
                if (!utcTimeString.endsWith('Z') && !utcTimeString.includes('+') && !utcTimeString.includes('-')) {
                    utcTimeString += 'Z';
                }
                utcDate = new Date(utcTimeString);
            } else if (utcTimeString instanceof Date) {
                utcDate = utcTimeString;
            } else {
                throw new Error('Invalid date format');
            }

            if (isNaN(utcDate.getTime())) {
                console.warn('Invalid date:', utcTimeString);
                return utcTimeString; // 如果日期无效，返回原字符串
            }

            return utcDate.toLocaleString(navigator.language, {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                timeZone: this.clientTimezone
            });
        } catch (error) {
            console.error('Error converting UTC to local time:', error);
            return utcTimeString;
        }
    }

    /**
     * 格式化时间为本地时间（更简洁的格式）
     */
    formatToLocalTime(utcTimeString, options = {}) {
        const defaults = {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        };
        
        const formatOptions = { ...defaults, ...options };

        try {
            let utcDate;
            if (typeof utcTimeString === 'string') {
                if (!utcTimeString.endsWith('Z') && !utcTimeString.includes('+') && !utcTimeString.includes('-')) {
                    utcTimeString += 'Z';
                }
                utcDate = new Date(utcTimeString);
            } else if (utcTimeString instanceof Date) {
                utcDate = utcTimeString;
            } else {
                return utcTimeString;
            }

            if (isNaN(utcDate.getTime())) {
                return utcTimeString;
            }

            return utcDate.toLocaleString(navigator.language, {
                ...formatOptions,
                timeZone: this.clientTimezone
            });
        } catch (error) {
            console.error('Error formatting time:', error);
            return utcTimeString;
        }
    }

    /**
     * 初始化页面上的所有时间元素
     */
    initializeTimeElements() {
        // 查找所有带有 data-utc-time 属性的元素
        const timeElements = document.querySelectorAll('[data-utc-time]');
        
        timeElements.forEach(element => {
            const utcTime = element.getAttribute('data-utc-time');
            const format = element.getAttribute('data-format') || 'full'; // 'full', 'date', 'time', 'datetime'
            const showTimezone = element.getAttribute('data-show-timezone'); // 'true' 显示时区, 'false' 不显示, 默认为 'false'

            if (utcTime) {
                let formattedTime;
                
                switch (format) {
                    case 'date':
                        formattedTime = this.formatToLocalTime(utcTime, {
                            year: 'numeric',
                            month: '2-digit',
                            day: '2-digit'
                        });
                        break;
                    case 'time':
                        formattedTime = this.formatToLocalTime(utcTime, {
                            hour: '2-digit',
                            minute: '2-digit'
                        });
                        break;
                    case 'datetime':
                        formattedTime = this.formatToLocalTime(utcTime, {
                            year: 'numeric',
                            month: '2-digit',
                            day: '2-digit',
                            hour: '2-digit',
                            minute: '2-digit'
                        });
                        break;
                    default: // 'full'
                        formattedTime = this.convertUtcToLocal(utcTime);
                }

                // 根据 data-show-timezone 属性决定是否显示时区信息
                const shouldShowTimezone = showTimezone === 'true';
                if (shouldShowTimezone) {
                    formattedTime += ` (${this.clientTimezone})`;
                }

                element.textContent = formattedTime;
            }
        });
    }

    /**
     * 获取当前客户端时区信息
     */
    getClientTimezoneInfo() {
        return {
            timezone: this.clientTimezone,
            offset: this.timezoneOffset,
            name: this.getTimezoneName()
        };
    }

    /**
     * 获取时区名称的简化版本
     */
    getTimezoneName() {
        const now = new Date();
        try {
            // 使用Intl.DateTimeFormat获取时区缩写
            const formatter = new Intl.DateTimeFormat('en', {
                timeZoneName: 'short',
                timeZone: this.clientTimezone
            });
            const parts = formatter.formatToParts(now);
            const timeZoneNamePart = parts.find(part => part.type === 'timeZoneName');
            return timeZoneNamePart ? timeZoneNamePart.value : this.clientTimezone;
        } catch (error) {
            return this.clientTimezone;
        }
    }
}

// 创建全局实例
const timezoneHelper = new TimezoneHelper();

// 页面加载完成后初始化时间元素
document.addEventListener('DOMContentLoaded', function() {
    timezoneHelper.initializeTimeElements();
});

// 如果页面内容是动态加载的，提供手动刷新方法
window.refreshTimeDisplays = function() {
    timezoneHelper.initializeTimeElements();
};

// 暴露到全局作用域
window.TimezoneHelper = timezoneHelper;

console.log('Timezone helper initialized. Client timezone:', timezoneHelper.getClientTimezoneInfo());