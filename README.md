
--> Forntend format to hit uploadImage Api:

const formData = new FormData();
formData.append('employeeId', 123);
formData.append('file', imageFile);

const response = await axios.post('/api/employee/profileImage', formData, {
  headers: {
    'Content-Type': 'multipart/form-data',
    'Authorization': `Bearer ${token}`
  }
});
----------------------------------------------------------------------------
