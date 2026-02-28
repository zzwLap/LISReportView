/**
 * 前端加密工具
 * 使用 CryptoJS 库进行 AES 加密，与后端 CryptoHelper 兼容
 * 需要先引入: <script src="~/lib/crypto-js/crypto-js.min.js"></script>
 */

// 默认密钥（应该与后端保持一致）
const DEFAULT_CRYPTO_KEY = "LisReportServer2026SecretKey!!";

/**
 * 获取有效的密钥字节（确保长度为16、24或32字节）
 * 与后端 GetValidKeyBytes 逻辑保持一致
 */
function getValidKeyBytes(key) {
    // 将字符串转为UTF-8字节
    const keyBytes = CryptoJS.enc.Utf8.parse(key);
    const keyLength = keyBytes.sigBytes;
    
    // AES支持16、24、32字节密钥
    let validLength;
    if (keyLength <= 16) {
        validLength = 16;
    } else if (keyLength <= 24) {
        validLength = 24;
    } else {
        validLength = 32;
    }
    
    // 创建有效长度的密钥
    const validKeyArray = new Uint8Array(validLength);
    const sourceArray = new Uint8Array(keyBytes.words.length * 4);
    
    // 将 WordArray 转换为 Uint8Array
    for (let i = 0; i < keyBytes.words.length; i++) {
        const word = keyBytes.words[i];
        sourceArray[i * 4] = (word >>> 24) & 0xff;
        sourceArray[i * 4 + 1] = (word >>> 16) & 0xff;
        sourceArray[i * 4 + 2] = (word >>> 8) & 0xff;
        sourceArray[i * 4 + 3] = word & 0xff;
    }
    
    // 填充密钥
    if (keyLength >= validLength) {
        validKeyArray.set(sourceArray.slice(0, validLength));
    } else {
        validKeyArray.set(sourceArray.slice(0, keyLength));
        // 如果密钥太短，用原密钥填充
        for (let i = keyLength; i < validLength; i++) {
            validKeyArray[i] = sourceArray[i % keyLength];
        }
    }
    
    // 转换回 WordArray
    const words = [];
    for (let i = 0; i < validLength; i += 4) {
        words.push(
            (validKeyArray[i] << 24) |
            (validKeyArray[i + 1] << 16) |
            (validKeyArray[i + 2] << 8) |
            validKeyArray[i + 3]
        );
    }
    
    return CryptoJS.lib.WordArray.create(words, validLength);
}

/**
 * AES 加密（与后端 CryptoHelper.Encrypt 兼容）
 * @param {string} plainText - 明文
 * @param {string} key - 密钥（可选）
 * @returns {string} Base64编码的密文
 */
function encryptPassword(plainText, key = DEFAULT_CRYPTO_KEY) {
    if (!plainText) return '';
    
    try {
        // 获取有效密钥
        const validKey = getValidKeyBytes(key);
        
        // 生成随机IV（16字节）
        const iv = CryptoJS.lib.WordArray.random(16);
        
        // AES加密：CBC模式，PKCS7填充
        const encrypted = CryptoJS.AES.encrypt(plainText, validKey, {
            iv: iv,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        });
        
        // 将IV和密文组合（与后端保持一致：IV在前，密文在后）
        const combinedData = iv.concat(encrypted.ciphertext);
        
        // 转换为Base64
        return CryptoJS.enc.Base64.stringify(combinedData);
    } catch (error) {
        console.error('加密失败:', error);
        // 加密失败则返回原文（向后兼容）
        return plainText;
    }
}

/**
 * 登录表单提交前加密密码
 * 使用方法：在表单的 onsubmit 事件中调用
 * 例如：<form onsubmit="return encryptLoginForm(this)">
 */
function encryptLoginForm(form) {
    const passwordField = form.querySelector('input[type="password"]');
    if (passwordField && passwordField.value) {
        // 创建隐藏字段存储加密后的密码
        let encryptedField = form.querySelector('input[name="Input.Password"]');
        if (!encryptedField) {
            encryptedField = document.createElement('input');
            encryptedField.type = 'hidden';
            encryptedField.name = 'Input.Password';
            form.appendChild(encryptedField);
        }
        
        // 加密密码
        encryptedField.value = encryptPassword(passwordField.value);
        
        // 清空原密码框（防止明文传输）
        passwordField.value = '';
        passwordField.removeAttribute('name'); // 移除name属性，防止提交
        
        return true;
    }
    return true;
}

/**
 * 为登录表单自动添加加密功能
 * 页面加载后自动执行
 */
document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.querySelector('form[method="post"]');
    if (loginForm) {
        // 检查是否包含密码字段
        const passwordField = loginForm.querySelector('input[type="password"]');
        if (passwordField) {
            loginForm.addEventListener('submit', function(e) {
                // 加密密码
                if (passwordField.value) {
                    const encrypted = encryptPassword(passwordField.value);
                    
                    // 创建或更新隐藏字段
                    let hiddenField = loginForm.querySelector('input[name="Input.Password"][type="hidden"]');
                    if (!hiddenField) {
                        hiddenField = document.createElement('input');
                        hiddenField.type = 'hidden';
                        hiddenField.name = 'Input.Password';
                        loginForm.appendChild(hiddenField);
                    }
                    hiddenField.value = encrypted;
                    
                    // 移除原始密码字段的name属性
                    passwordField.setAttribute('data-original-name', passwordField.name);
                    passwordField.removeAttribute('name');
                    
                    console.log('密码已加密');
                }
            });
        }
    }
});
