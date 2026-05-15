const api = require('../../utils/api');

Page({
  data: {
    foodList: [],
    stats: {
      normal: 0,
      expiringSoon: 0,
      expired: 0
    },
    loading: true
  },

  onShow() {
    this.loadFoodList();
  },

  onPullDownRefresh() {
    this.loadFoodList().then(() => {
      wx.stopPullDownRefresh();
    });
  },

  async loadFoodList() {
    this.setData({ loading: true });
    try {
      const [foodList, stats] = await Promise.all([
        api.getFoodList(),
        api.getFoodStats()
      ]);

      this.setData({
        foodList: foodList || [],
        stats: stats || { normal: 0, expiringSoon: 0, expired: 0 },
        loading: false
      });
    } catch (err) {
      // 未登录情况
      const token = wx.getStorageSync('token');
      if (!token) {
        this.setData({ foodList: [], stats: { normal: 0, expiringSoon: 0, expired: 0 }, loading: false });
        return;
      }
      this.setData({ loading: false });
      wx.showToast({ title: '加载失败', icon: 'none' });
    }
  },

  formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}.${m}.${day}`;
  },

  getCategoryEmoji(category) {
    const map = {
      '肉类': '🥩',
      '蔬菜': '🥬',
      '水果': '🍎',
      '乳制品': '🥛',
      '饮料': '🧃',
      '调味品': '🧂'
    };
    return map[category] || '📦';
  },

  goToDetail(e) {
    // 当前版本点击卡片暂不跳转详情页
    // 可扩展为详情编辑页
  }
});
