import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

// Image Models matching backend DTOs
export interface ImageUploadRequest {
  file: File;
  fileType: string;
  description?: string;
}

export interface ImageFileDto {
  id: number;
  fileName: string;
  originalFileName: string;
  filePath: string;
  contentType: string;
  fileSizeBytes: number;
  fileHash: string;
  width: number;
  height: number;
  fileType: string;
  uploadedBy: string;
  uploadedByName: string;
  uploadedAt: Date;
  isActive: boolean;
  thumbnailPath?: string;
  publicUrl: string;
}

export interface EmployeePhotoDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  imageFile: ImageFileDto;
  setBy: string;
  setByName: string;
  setAt: Date;
  isActive: boolean;
  notes?: string;
}

export interface AttendancePhotoDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  imageFile: ImageFileDto;
  attendanceEventId?: number;
  photoType: string;
  takenAt: Date;
  location?: string;
  latitude?: number;
  longitude?: number;
  deviceInfo?: string;
  isVerified: boolean;
  verifiedBy?: string;
  verifiedByName?: string;
  verifiedAt?: Date;
  notes?: string;
  isFaceMatched?: boolean;
  faceConfidenceScore?: number;
}

export interface LeaveRequestAttachmentDto {
  id: number;
  leaveRequestId: number;
  imageFile: ImageFileDto;
  attachmentType: string;
  attachmentTypeName: string;
  description?: string;
  attachedAt: Date;
  isRequired: boolean;
  isApproved: boolean;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: Date;
  approvalNotes?: string;
}

export interface AttendancePhotoRequest {
  photoFile: File;
  photoType: string;
  attendanceEventId?: number;
  latitude?: number;
  longitude?: number;
  location?: string;
  deviceInfo?: string;
  notes?: string;
}

export interface TopUploaderDto {
  employeeCode: string;
  employeeName: string;
  uploadCount: number;
  totalSizeBytes: number;
  totalSizeFormatted: string;
}

export interface ImageStatistics {
  totalImages: number;
  totalSizeBytes: number;
  totalSizeFormatted: string;
  activeImages: number;
  deletedImages: number;
  imagesByType: { [key: string]: number };
  imagesByContentType: { [key: string]: number };
  employeesWithPhotos: number;
  totalEmployees: number;
  photoCompletionPercentage: number;
  attendancePhotosToday: number;
  attendancePhotosThisWeek: number;
  attendancePhotosThisMonth: number;
  leaveAttachmentsThisMonth: number;
  lastImageUpload: Date;
  lastUploadedBy?: string;
  topUploaders: TopUploaderDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ImageService {
  private readonly API_URL = `${environment.apiUrl}/image`;

  constructor(private http: HttpClient) {}

  // Basic Image Operations
  uploadImage(file: File, fileType: string, description?: string): Observable<ApiResponse<ImageFileDto>> {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('FileType', fileType);
    if (description) {
      formData.append('Description', description);
    }

    return this.http.post<ApiResponse<ImageFileDto>>(`${this.API_URL}/upload`, formData);
  }

  viewImage(imageId: number): string {
    return `${environment.apiUrl}/image/view/${imageId}`;
  }

  deleteImage(imageId: number, reason?: string): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (reason) {
      params = params.set('reason', reason);
    }

    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/${imageId}`, { params });
  }

  // Employee Photo Management
  setEmployeePhoto(employeeCode: string, photoFile: File): Observable<ApiResponse<EmployeePhotoDto>> {
    const formData = new FormData();
    formData.append('photoFile', photoFile);

    return this.http.post<ApiResponse<EmployeePhotoDto>>(`${this.API_URL}/employee-photo/${employeeCode}`, formData);
  }

  getEmployeePhoto(employeeCode: string): string {
    return `${environment.apiUrl}/image/employee-photo/${employeeCode}`;
  }

  setMyPhoto(photoFile: File): Observable<ApiResponse<EmployeePhotoDto>> {
    const formData = new FormData();
    formData.append('photoFile', photoFile);

    return this.http.post<ApiResponse<EmployeePhotoDto>>(`${this.API_URL}/my-photo`, formData);
  }

  getMyPhoto(): string {
    return `${environment.apiUrl}/image/my-photo`;
  }

  // Attendance Photo Management
  saveAttendancePhoto(request: AttendancePhotoRequest): Observable<ApiResponse<AttendancePhotoDto>> {
    const formData = new FormData();
    formData.append('PhotoFile', request.photoFile);
    formData.append('PhotoType', request.photoType);
    
    if (request.attendanceEventId !== undefined) {
      formData.append('AttendanceEventId', request.attendanceEventId.toString());
    }
    if (request.latitude !== undefined) {
      formData.append('Latitude', request.latitude.toString());
    }
    if (request.longitude !== undefined) {
      formData.append('Longitude', request.longitude.toString());
    }
    if (request.location) {
      formData.append('Location', request.location);
    }
    if (request.deviceInfo) {
      formData.append('DeviceInfo', request.deviceInfo);
    }
    if (request.notes) {
      formData.append('Notes', request.notes);
    }

    return this.http.post<ApiResponse<AttendancePhotoDto>>(`${this.API_URL}/attendance-photo`, formData);
  }

  getMyAttendancePhotos(date?: Date, photoType?: string): Observable<ApiResponse<AttendancePhotoDto[]>> {
    let params = new HttpParams();
    
    if (date) {
      params = params.set('date', date.toISOString().split('T')[0]);
    }
    if (photoType) {
      params = params.set('photoType', photoType);
    }

    return this.http.get<ApiResponse<AttendancePhotoDto[]>>(`${this.API_URL}/attendance-photos`, { params });
  }

  getEmployeeAttendancePhotos(
    employeeCode: string, 
    startDate?: Date, 
    endDate?: Date, 
    photoType?: string
  ): Observable<ApiResponse<AttendancePhotoDto[]>> {
    let params = new HttpParams();
    
    if (startDate) {
      params = params.set('startDate', startDate.toISOString().split('T')[0]);
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString().split('T')[0]);
    }
    if (photoType) {
      params = params.set('photoType', photoType);
    }

    return this.http.get<ApiResponse<AttendancePhotoDto[]>>(`${this.API_URL}/attendance-photos/${employeeCode}`, { params });
  }

  verifyAttendancePhoto(photoId: number, verificationData: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/attendance-photos/${photoId}/verify`, verificationData);
  }

  // Leave Request Attachments
  addLeaveAttachment(
    leaveRequestId: number,
    file: File,
    attachmentType: string,
    description?: string
  ): Observable<ApiResponse<LeaveRequestAttachmentDto>> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('attachmentType', attachmentType);
    
    if (description) {
      formData.append('description', description);
    }

    return this.http.post<ApiResponse<LeaveRequestAttachmentDto>>(`${this.API_URL}/leave-attachment/${leaveRequestId}`, formData);
  }

  getLeaveAttachments(leaveRequestId: number): Observable<ApiResponse<LeaveRequestAttachmentDto[]>> {
    return this.http.get<ApiResponse<LeaveRequestAttachmentDto[]>>(`${this.API_URL}/leave-attachments/${leaveRequestId}`);
  }

  // Image Statistics & Management
  getImageStatistics(): Observable<ApiResponse<ImageStatistics>> {
    return this.http.get<ApiResponse<ImageStatistics>>(`${this.API_URL}/statistics`);
  }

  cleanupOldImages(olderThanDays: number = 365): Observable<ApiResponse<any>> {
    const params = new HttpParams().set('olderThanDays', olderThanDays.toString());
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/cleanup`, null, { params });
  }

  // Helper methods
  getImageUrl(imageId: number): string {
    return this.viewImage(imageId);
  }

  getEmployeeAvatarUrl(employeeCode: string): string {
    return this.getEmployeePhoto(employeeCode);
  }

  isImageFile(file: File): boolean {
    return file.type.startsWith('image/');
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
