const api = require('../../utils/api');
const auth = require('../../utils/auth');

Page({
  data: {
    categories: ['肉类', '蔬菜', '水果', '乳制品', '饮料', '调味品', '其他'],
    units: ['个', '袋', '盒', '瓶', '罐', '包', '斤', '克', '升', 'ml'],
    categoryIndex: -1,
    unitIndex: 0,
    ocrProcessing: false,
    ocrResult: null,
    submitting: false,
    form: {
      name: '',
      expirationDate: '',
      quantity: '1',
      unit: '个',
      notes: ''
    }
  },

  onLoad() {
    // 设置默认日期为今天 + 7天
    const d = new Date();
    d.setDate(d.getDate() + 7);
    this.setData({
      'form.expirationDate': this.formatDateInput(d)
    });
  },

  // ===== 拍照 & OCR =====
  takePhoto() {
    // 检查登录
    if (!auth.isLoggedIn()) {
      wx.showToast({ title: '请先登录', icon: 'none' });
      wx.switchTab({ url: '/pages/profile/profile' });
      return;
    }

    const that = this;
    wx.chooseMedia({
      count: 1,
      mediaType: ['image'],
      sourceType: ['camera', 'album'],
      sizeType: ['compressed'],
      success(res) {
        const tempFilePath = res.tempFiles[0].tempFilePath;
        that.processOCR(tempFilePath);
      },
      fail(err) {
        if (err.errMsg !== 'chooseMedia:fail cancel') {
          wx.showToast({ title: '拍照失败', icon: 'none' });
        }
      }
    });
  },

  async processOCR(filePath) {
    this.setData({
      ocrProcessing: true,
      ocrResult: null
    });

    try {
      const result = await api.recognizeImage(filePath);

      if (result.success) {
        const form = { ...this.data.form };
        if (result.foodName) form.name = result.foodName;
        if (result.expirationDate) {
          const d = new Date(result.expirationDate);
          form.expirationDate = this.formatDateInput(d);
        }

        this.setData({
          form,
          ocrResult: result,
          ocrProcessing: false
        });

        wx.showToast({ title: '识别成功', icon: 'success' });
      } else {
        this.setData({
          ocrResult: result,
          ocrProcessing: false
        });
        wx.showToast({ title: result.errorMessage || '识别失败', icon: 'none' });
      }
    } catch (err) {
      this.setData({ ocrProcessing: false });
      wx.showToast({ title: 'OCR 服务异常', icon: 'none' });
    }
  },

  // ===== 表单事件 =====
  onNameInput(e) {
    this.setData({ 'form.name': e.detail.value });
  },

  onCategoryChange(e) {
    const index = e.detail.value;
    this.setData({
      categoryIndex: index,
      'form.category': this.data.categories[index]
    });
  },

  onDateChange(e) {
    this.setData({ 'form.expirationDate': e.detail.value });
  },

  onQuantityInput(e) {
    this.setData({ 'form.quantity': e.detail.value || '1' });
  },

  onUnitChange(e) {
    const index = e.detail.value;
    this.setData({
      unitIndex: index,
      'form.unit': this.data.units[index]
    });
  },

  onNotesInput(e) {
    this.setData({ 'form.notes': e.detail.value });
  },

  // ===== 提交 =====
  async submitForm() {
    const form = this.data.form;

    if (!form.name.trim()) {
      wx.showToast({ title: '请输入食品名称', icon: 'none' });
      return;
    }
    if (!form.expirationDate) {
      wx.showToast({ title: '请选择保质期', icon: 'none' });
      return;
    }

    // 登录检查
    if (!auth.isLoggedIn()) {
      wx.showToast({ title: '请先登录', icon: 'none' });
      wx.switchTab({ url: '/pages/profile/profile' });
      return;
    }

    this.setData({ submitting: true });

    try {
      await api.addFood({
        name: form.name.trim(),
        category: form.category || null,
        expirationDate: form.expirationDate,
        quantity: parseInt(form.quantity) || 1,
        unit: form.unit || '个',
        notes: form.notes || null
      });

      wx.showToast({ title: '添加成功', icon: 'success' });
      // 清空表单
      this.setData({
        form: {
          name: '',
          expirationDate: this.formatDateInput(new Date(Date.now() + 7 * 86400000)),
          quantity: '1',
          unit: '个',
          notes: ''
        },
        categoryIndex: -1,
        unitIndex: 0,
        ocrResult: null,
        submitting: false
      });

      // 跳转到首页
      setTimeout(() => {
        wx.switchTab({ url: '/pages/index/index' });
      }, 500);
    } catch (err) {
      this.setData({ submitting: false });
      wx.showToast({ title: '保存失败', icon: 'none' });
    }
  },

  // 工具函数
  formatDateInput(d) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
});
