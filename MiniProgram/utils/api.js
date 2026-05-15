// API 基础配置
const API_BASE_URL = 'http://localhost:5211'; // 修改为你实际的API地址
const TOKEN_KEY = 'token';

// 通用请求封装
function request(url, method = 'GET', data = {}, needAuth = true) {
  return new Promise((resolve, reject) => {
    const header = { 'Content-Type': 'application/json' };

    if (needAuth) {
      const token = wx.getStorageSync(TOKEN_KEY);
      if (token) {
        header['Authorization'] = `Bearer ${token}`;
      }
    }

    wx.request({
      url: `${API_BASE_URL}${url}`,
      method: method,
      data: data,
      header: header,
      success(res) {
        if (res.statusCode === 401) {
          // Token 过期，跳转登录
          wx.removeStorageSync(TOKEN_KEY);
          wx.showToast({ title: '登录已过期，请重新登录', icon: 'none' });
          reject(new Error('Unauthorized'));
          return;
        }
        resolve(res.data);
      },
      fail(err) {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        reject(err);
      }
    });
  });
}

module.exports = {
  API_BASE_URL,

  // ===== 认证接口 =====
  login(code, nickName, avatarUrl) {
    return request('/api/auth/login', 'POST', {
      code,
      nickName,
      avatarUrl
    }, false);
  },

  // ===== 食品接口 =====
  getFoodList() {
    return request('/api/food', 'GET');
  },

  getFoodDetail(id) {
    return request(`/api/food/${id}`, 'GET');
  },

  addFood(data) {
    return request('/api/food', 'POST', data);
  },

  updateFood(id, data) {
    return request(`/api/food/${id}`, 'PUT', data);
  },

  deleteFood(id) {
    return request(`/api/food/${id}`, 'DELETE');
  },

  getFoodStats() {
    return request('/api/food/stats', 'GET');
  },

  // ===== OCR 接口 =====
  recognizeImage(filePath) {
    return new Promise((resolve, reject) => {
      const token = wx.getStorageSync(TOKEN_KEY);
      wx.uploadFile({
        url: `${API_BASE_URL}/api/ocr/recognize`,
        filePath: filePath,
        name: 'file',
        header: {
          'Authorization': `Bearer ${token}`
        },
        success(res) {
          try {
            const data = JSON.parse(res.data);
            resolve(data);
          } catch (e) {
            reject(e);
          }
        },
        fail(err) {
          reject(err);
        }
      });
    });
  }
};
