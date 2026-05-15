const api = require('./api');

// 微信登录流程
function wechatLogin() {
  return new Promise((resolve, reject) => {
    // 1. 获取微信登录 code
    wx.login({
      success(res) {
        if (res.code) {
          // 2. 获取用户信息
          wx.getUserProfile({
            desc: '用于完善用户资料',
            success(profileRes) {
              const userInfo = profileRes.userInfo;
              // 3. 调用后端登录接口
              api.login(res.code, userInfo.nickName, userInfo.avatarUrl)
                .then(loginRes => {
                  // 4. 保存 Token 和用户信息
                  wx.setStorageSync('token', loginRes.token);
                  wx.setStorageSync('userInfo', {
                    nickName: loginRes.nickName || userInfo.nickName,
                    avatarUrl: loginRes.avatarUrl || userInfo.avatarUrl
                  });
                  resolve(loginRes);
                })
                .catch(err => reject(err));
            },
            fail() {
              // 用户拒绝授权，使用默认头像
              api.login(res.code, '用户', '')
                .then(loginRes => {
                  wx.setStorageSync('token', loginRes.token);
                  wx.setStorageSync('userInfo', {
                    nickName: '用户',
                    avatarUrl: ''
                  });
                  resolve(loginRes);
                })
                .catch(err => reject(err));
            }
          });
        } else {
          reject(new Error('微信登录失败'));
        }
      },
      fail(err) {
        reject(err);
      }
    });
  });
}

// 检查是否已登录
function isLoggedIn() {
  const token = wx.getStorageSync('token');
  return !!token;
}

module.exports = {
  wechatLogin,
  isLoggedIn
};
