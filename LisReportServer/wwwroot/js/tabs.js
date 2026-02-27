/**
 * 标签页管理器
 * 提供多标签页打开、切换、关闭等功能
 */
const tabManager = {
    tabs: [],
    activeTabId: null,
    tabCounter: 0,
    maxTabs: 20, // 最大标签页数量

    /**
     * 初始化标签页管理器
     */
    init: function() {
        this.tabList = document.getElementById('tab-list');
        this.contentContainer = document.getElementById('tab-content-container');
        
        // 绑定键盘事件
        this.bindKeyboardEvents();
        
        // 绑定窗口大小变化事件
        window.addEventListener('resize', () => {
            this.updateScrollButtons();
        });
    },

    /**
     * 打开新标签页
     * @param {string} title - 标签页标题
     * @param {string} url - 页面URL
     * @param {object} options - 其他选项
     */
    openTab: function(title, url, options = {}) {
        // 检查是否已存在相同URL的标签页
        const existingTab = this.tabs.find(tab => tab.url === url);
        if (existingTab) {
            this.switchTab(existingTab.id);
            return existingTab.id;
        }

        // 检查是否超出最大标签页数量
        if (this.tabs.length >= this.maxTabs) {
            // 关闭最早打开的非首页标签
            const tabToClose = this.tabs.find(tab => !tab.isHome);
            if (tabToClose) {
                this.closeTab(tabToClose.id);
            }
        }

        // 生成标签页ID
        this.tabCounter++;
        const tabId = 'tab-' + this.tabCounter;

        // 创建标签页数据
        const tab = {
            id: tabId,
            title: title,
            url: url,
            isHome: url === '/Index' || url === '/',
            options: options
        };

        this.tabs.push(tab);

        // 创建标签页DOM
        this.createTabElement(tab);

        // 创建内容区域DOM（使用iframe）
        this.createContentElement(tab);

        // 切换到新标签页
        this.switchTab(tabId);

        return tabId;
    },

    /**
     * 创建标签页DOM元素
     */
    createTabElement: function(tab) {
        const tabElement = document.createElement('div');
        tabElement.className = 'tab-item' + (tab.isHome ? ' home-tab' : '');
        tabElement.id = tab.id;
        tabElement.onclick = (e) => {
            if (!e.target.closest('.tab-close') && !e.target.closest('.tab-menu-btn')) {
                this.switchTab(tab.id);
            }
        };

        // 图标
        let icon = '';
        if (tab.isHome) {
            icon = '<i class="bi bi-house-door tab-icon"></i>';
        } else if (tab.url.includes('Dashboard')) {
            icon = '<i class="bi bi-speedometer2 tab-icon"></i>';
        } else if (tab.url.includes('Reports')) {
            icon = '<i class="bi bi-file-earmark-text tab-icon"></i>';
        } else if (tab.url.includes('Admin')) {
            icon = '<i class="bi bi-gear tab-icon"></i>';
        } else if (tab.url.includes('Health')) {
            icon = '<i class="bi bi-heart-pulse tab-icon"></i>';
        } else {
            icon = '<i class="bi bi-file-earmark tab-icon"></i>';
        }

        tabElement.innerHTML = `
            ${icon}
            <span class="tab-title">${this.escapeHtml(tab.title)}</span>
            ${!tab.isHome ? '<span class="tab-close" onclick="tabManager.closeTab(\'' + tab.id + '\'); event.stopPropagation();"><i class="bi bi-x"></i></span>' : ''}
        `;

        this.tabList.appendChild(tabElement);
        this.updateScrollButtons();
    },

    /**
     * 创建内容区域DOM元素
     */
    createContentElement: function(tab) {
        const contentElement = document.createElement('div');
        contentElement.className = 'tab-content';
        contentElement.id = 'content-' + tab.id;
        // 使用iframe加载页面，添加content=true参数使用简化布局
        const contentUrl = tab.url.includes('?') ? tab.url + '&content=true' : tab.url + '?content=true';
        contentElement.innerHTML = `<iframe src="${contentUrl}" frameborder="0" scrolling="auto"></iframe>`;
        this.contentContainer.appendChild(contentElement);
    },

    /**
     * 切换标签页
     */
    switchTab: function(tabId) {
        // 隐藏所有标签页内容
        document.querySelectorAll('.tab-content').forEach(el => {
            el.classList.remove('active');
        });
        document.querySelectorAll('.tab-item').forEach(el => {
            el.classList.remove('active');
        });

        // 显示当前标签页
        const tabElement = document.getElementById(tabId);
        const contentElement = document.getElementById('content-' + tabId);
        
        if (tabElement && contentElement) {
            tabElement.classList.add('active');
            contentElement.classList.add('active');
            this.activeTabId = tabId;
            
            // 滚动标签到可视区域
            tabElement.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
        }
    },

    /**
     * 关闭标签页
     */
    closeTab: function(tabId) {
        const tabIndex = this.tabs.findIndex(tab => tab.id === tabId);
        if (tabIndex === -1) return;

        const tab = this.tabs[tabIndex];
        
        // 首页标签不能关闭
        if (tab.isHome) {
            return;
        }

        // 移除DOM元素
        const tabElement = document.getElementById(tabId);
        const contentElement = document.getElementById('content-' + tabId);
        
        if (tabElement) tabElement.remove();
        if (contentElement) contentElement.remove();

        // 从数组中移除
        this.tabs.splice(tabIndex, 1);

        // 如果关闭的是当前活动标签，切换到其他标签
        if (this.activeTabId === tabId) {
            if (this.tabs.length > 0) {
                // 优先切换到右边的标签，没有则切换到左边
                const newTabIndex = tabIndex < this.tabs.length ? tabIndex : this.tabs.length - 1;
                this.switchTab(this.tabs[newTabIndex].id);
            }
        }

        this.updateScrollButtons();
    },

    /**
     * 刷新标签页
     */
    refreshTab: function(tabId) {
        const tab = this.tabs.find(t => t.id === tabId);
        if (tab) {
            const contentElement = document.getElementById('content-' + tabId);
            const contentUrl = tab.url.includes('?') ? tab.url + '&content=true' : tab.url + '?content=true';
            contentElement.innerHTML = `<iframe src="${contentUrl}" frameborder="0" scrolling="auto"></iframe>`;
        }
    },

    /**
     * 关闭所有标签页（保留首页）
     */
    closeAllTabs: function() {
        const tabsToClose = this.tabs.filter(tab => !tab.isHome);
        tabsToClose.forEach(tab => {
            this.closeTab(tab.id);
        });
    },

    /**
     * 关闭其他标签页（保留当前和首页）
     */
    closeOtherTabs: function(tabId) {
        const tabsToClose = this.tabs.filter(tab => tab.id !== tabId && !tab.isHome);
        tabsToClose.forEach(tab => {
            this.closeTab(tab.id);
        });
    },

    /**
     * 滚动标签列表
     */
    scrollTabs: function(direction) {
        const scrollAmount = 200;
        if (direction === 'left') {
            this.tabList.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
        } else {
            this.tabList.scrollBy({ left: scrollAmount, behavior: 'smooth' });
        }
    },

    /**
     * 更新滚动按钮状态
     */
    updateScrollButtons: function() {
        const scrollLeftBtn = document.querySelector('.tab-scroll-left');
        const scrollRightBtn = document.querySelector('.tab-scroll-right');
        
        if (!scrollLeftBtn || !scrollRightBtn) return;

        const isOverflow = this.tabList.scrollWidth > this.tabList.clientWidth;
        
        if (isOverflow) {
            scrollLeftBtn.classList.remove('hidden');
            scrollRightBtn.classList.remove('hidden');
        } else {
            scrollLeftBtn.classList.add('hidden');
            scrollRightBtn.classList.add('hidden');
        }
    },

    /**
     * 绑定键盘事件
     */
    bindKeyboardEvents: function() {
        document.addEventListener('keydown', (e) => {
            // Ctrl+W 关闭当前标签页
            if (e.ctrlKey && e.key === 'w') {
                e.preventDefault();
                if (this.activeTabId) {
                    this.closeTab(this.activeTabId);
                }
            }
            
            // Ctrl+Tab 切换到下一个标签页
            if (e.ctrlKey && e.key === 'Tab') {
                e.preventDefault();
                this.switchToNextTab();
            }
            
            // Ctrl+Shift+Tab 切换到上一个标签页
            if (e.ctrlKey && e.shiftKey && e.key === 'Tab') {
                e.preventDefault();
                this.switchToPrevTab();
            }
        });
    },

    /**
     * 切换到下一个标签页
     */
    switchToNextTab: function() {
        if (!this.activeTabId) return;
        const currentIndex = this.tabs.findIndex(tab => tab.id === this.activeTabId);
        const nextIndex = (currentIndex + 1) % this.tabs.length;
        this.switchTab(this.tabs[nextIndex].id);
    },

    /**
     * 切换到上一个标签页
     */
    switchToPrevTab: function() {
        if (!this.activeTabId) return;
        const currentIndex = this.tabs.findIndex(tab => tab.id === this.activeTabId);
        const prevIndex = (currentIndex - 1 + this.tabs.length) % this.tabs.length;
        this.switchTab(this.tabs[prevIndex].id);
    },

    /**
     * HTML转义
     */
    escapeHtml: function(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * 获取当前活动的标签页ID
     */
    getActiveTabId: function() {
        return this.activeTabId;
    },

    /**
     * 获取所有标签页
     */
    getAllTabs: function() {
        return this.tabs;
    }
};

// 导出模块（如果使用模块系统）
if (typeof module !== 'undefined' && module.exports) {
    module.exports = tabManager;
}
