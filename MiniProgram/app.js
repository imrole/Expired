App({
  globalData: {
    userInfo: null,
    token: ''
  },

  onLaunch() {
    // 检查本地是否有 Token
    const token = wx.getStorageSync('token');
    const userInfo = wx.getStorageSync('userInfo');
    if (token) {
      this.globalData.token = token;
      this.globalData.userInfo = userInfo;
    }
  }
});
