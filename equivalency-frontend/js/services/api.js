// API Service Layer
// Configures Axios with base URL and automatic JWT token attachment

const API_BASE_URL = 'https://localhost:7012/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Attach JWT token to every request automatically
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

// Handle 401 responses globally (redirect to login)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('role');
      localStorage.removeItem('userName');
      window.location.hash = '#/login';
    }
    return Promise.reject(error);
  }
);

// ========== Auth API ==========
const authAPI = {
  login: (email, password) => api.post('/auth/login', { email, password }),
};

// ========== Student Courses API ==========
const studentCoursesAPI = {
  getAll: () => api.get('/studentcourses'),
  add: (courseData) => {
    if (courseData instanceof FormData) {
      // لا تقم بتحديد 'Content-Type': 'multipart/form-data' يدوياً
      // لأن Axios يقوم بذلك تلقائياً ويضيف الـ boundary الخاص بالملف
      // إضافته يدوياً تفسد إرسال الملف!
      return api.post('/studentcourses', courseData);
    }
    return api.post('/studentcourses', courseData);
  },
  delete: (id) => api.delete(`/studentcourses/${id}`),
};

// ========== Universities API ==========
const universitiesAPI = {
  getAll: () => api.get('/universities'),
};

// ========== Requests API ==========
const requestsAPI = {
  getAll: () => api.get('/requests'),
  submit: (requestData) => api.post('/requests', requestData),
  getMyRequests: () => api.get('/requests/my'),
  approve: (id) => api.put(`/requests/${id}/approve`),
  reject: (id) => api.put(`/requests/${id}/reject`),
  getPending: () => api.get('/requests/pending'),
};

// ========== Courses API (University courses for equivalency) ==========
const coursesAPI = {
  getAll: () => api.get('/courses'),
};
