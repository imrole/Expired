const api = require('../../utils/api');
const auth = require('../../utils/auth');

Page({
  data: {
    isLoggedIn: false,
    userInfo: {
      nickName: '',
      avatarUrl: ''
    },
    stats: {
      total: 0,
      normal: 0,
      expiringSoon: 0,
      expired: 0
    }
  },

  onShow() {
    this.checkLoginStatus();
  },

  checkLoginStatus() {
    const loggedIn = auth.isLoggedIn();
    const userInfo = wx.getStorageSync('userInfo') || {};

    this.setData({
      isLoggedIn: loggedIn,
      userInfo: userInfo
    });

    if (loggedIn) {
      this.loadStats();
    }
  },

  async loadStats() {
    try {
      const stats = await api.getFoodStats();
      this.setData({ stats: stats || { total: 0, normal: 0, expiringSoon: 0, expired: 0 } });
    } catch (err) {
      // ignore
    }
  },

  // ===== 微信登录 =====
  async login() {
    wx.showLoading({ title: '登录中...' });
    try {
      const result = await auth.wechatLogin();
      wx.hideLoading();

      this.setData({
        isLoggedIn: true,
        userInfo: {
          nickName: result.nickName || '用户',
          avatarUrl: result.avatarUrl || ''
        }
      });

      wx.showToast({ title: '登录成功', icon: 'success' });
      this.loadStats();
    } catch (err) {
      wx.hideLoading();
      wx.showToast({ title: '登录失败，请重试', icon: 'none' });
    }
  },

  // ===== 退出登录 =====
  logout() {
    wx.showModal({
      title: '确认退出',
      content: '退出后需要重新登录才能管理食品',
      success: (res) => {
        if (res.confirm) {
          wx.removeStorageSync('token');
          wx.removeStorageSync('userInfo');
          this.setData({
            isLoggedIn: false,
            userInfo: { nickName: '', avatarUrl: '' },
            stats: { total: 0, normal: 0, expiringSoon: 0, expired: 0 }
          });
          wx.showToast({ title: '已退出登录', icon: 'success' });
        }
      }
    });
  },

  // ===== 一键清理过期食品 =====
  async clearExpired() {
    if (!this.data.isLoggedIn) {
      wx.showToast({ title: '请先登录', icon: 'none' });
      return;
    }

    wx.showModal({
      title: '清理过期食品',
      content: '确定要删除所有已过期的食品记录吗？',
      success: async (res) => {
        if (res.confirm) {
          try {
            const foodList = await api.getFoodList();
            const expiredItems = (foodList || []).filter(f => f.status === 2);

            if (expiredItems.length === 0) {
              wx.showToast({ title: '没有过期食品', icon: 'none' });
              return;
            }

            // 逐条删除
            for (const item of expiredItems) {
              await api.deleteFood(item.id);
            }

            wx.showToast({
              title: `已清理 ${expiredItems.length} 项`,
              icon: 'success'
            });
            this.loadStats();
          } catch (err) {
            wx.showToast({ title: '清理失败', icon: 'none' });
          }
        }
      }
    });
  },

  // ===== 关于 =====
  about() {
    wx.showModal({
      title: '关于冰箱管家',
      content: '冰箱管家 v1.0\n\n记录冰箱内食品的保质期，\n帮助你合理安排食材使用，\n减少食物浪费。\n\n技术栈：微信小程序 + .NET + SQL Server',
      showCancel: false
    });
  }
});
