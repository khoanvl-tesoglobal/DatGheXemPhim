-- =========================
-- Full script tạo CinemaDB + seed ghế (mọi ghế = 'Trong')
-- Idempotent: có thể chạy nhiều lần mà không gây lỗi duplicate
-- =========================

-- 1) Tạo database nếu chưa có (không DROP DB để tránh lỗi "in use")
IF DB_ID(N'CinemaDB') IS NULL
BEGIN
    CREATE DATABASE CinemaDB;
    PRINT 'Created database CinemaDB.';
END
GO

-- 2) Chuyển ngữ cảnh sang CinemaDB
USE CinemaDB;
GO

-- 3) Xóa bảng nếu đã tồn tại để tái tạo cấu trúc sạch (an toàn trong dev)
IF OBJECT_ID(N'dbo.Ghe', N'U') IS NOT NULL
    DROP TABLE dbo.Ghe;
IF OBJECT_ID(N'dbo.LoaiGhe', N'U') IS NOT NULL
    DROP TABLE dbo.LoaiGhe;
GO

-- 4) Tạo bảng LoaiGhe
CREATE TABLE LoaiGhe (
    MaLoai CHAR(2) PRIMARY KEY,
    TenLoai NVARCHAR(50) NOT NULL
);
GO

-- 5) Chèn dữ liệu loại ghế (chỉ chèn khi chưa có)
IF NOT EXISTS (SELECT 1 FROM LoaiGhe WHERE MaLoai = 'T')
    INSERT INTO LoaiGhe (MaLoai, TenLoai) VALUES ('T', N'Thuong');
IF NOT EXISTS (SELECT 1 FROM LoaiGhe WHERE MaLoai = 'V')
    INSERT INTO LoaiGhe (MaLoai, TenLoai) VALUES ('V', N'VIP');
IF NOT EXISTS (SELECT 1 FROM LoaiGhe WHERE MaLoai = 'S')
    INSERT INTO LoaiGhe (MaLoai, TenLoai) VALUES ('S', N'SweetBox');
GO

-- 6) Tạo bảng Ghe phù hợp với code C# (cột Loai và TrangThai là chuỗi)
CREATE TABLE Ghe (
    MaGhe NVARCHAR(5) PRIMARY KEY,   -- ví dụ 'A1', 'K15'
    Hang CHAR(1) NOT NULL,
    SoGhe INT NOT NULL,
    Loai NVARCHAR(20) NOT NULL,      -- 'Thuong' | 'VIP' | 'SweetBox'
    TrangThai NVARCHAR(20) NOT NULL DEFAULT (N'Trong')  -- 'Trong' | 'DaChon' | 'KhongTheChon'
);
GO

-- 7) Seed dữ liệu ghế (khai báo biến đúng phạm vi, kiểm tra tồn tại để tránh duplicate)
-- Hàng A-D (Thuong) - 13 ghế mỗi hàng
DECLARE @row CHAR(1) = 'A';
WHILE @row <= 'D'
BEGIN
    DECLARE @i INT = 1;
    WHILE @i <= 13
    BEGIN
        DECLARE @ma NVARCHAR(5) = @row + CAST(@i AS NVARCHAR(3));
        IF NOT EXISTS (SELECT 1 FROM Ghe WHERE MaGhe = @ma)
        BEGIN
            INSERT INTO Ghe (MaGhe, Hang, SoGhe, Loai)
            VALUES (@ma, @row, @i, N'Thuong');
        END
        SET @i = @i + 1;
    END
    SET @row = CHAR(ASCII(@row) + 1);
END
GO

-- Hàng E,F,G,H,J (VIP) - 13 ghế mỗi hàng
DECLARE @rowsVip TABLE (Hang CHAR(1));
INSERT INTO @rowsVip (Hang) VALUES ('E'),('F'),('G'),('H'),('J');

DECLARE @r CHAR(1);
DECLARE vip_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Hang FROM @rowsVip;
OPEN vip_cursor;
FETCH NEXT FROM vip_cursor INTO @r;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @i2 INT = 1;
    WHILE @i2 <= 13
    BEGIN
        DECLARE @mag NVARCHAR(5) = @r + CAST(@i2 AS NVARCHAR(3));
        IF NOT EXISTS (SELECT 1 FROM Ghe WHERE MaGhe = @mag)
        BEGIN
            INSERT INTO Ghe (MaGhe, Hang, SoGhe, Loai)
            VALUES (@mag, @r, @i2, N'VIP');
        END
        SET @i2 = @i2 + 1;
    END
    FETCH NEXT FROM vip_cursor INTO @r;
END
CLOSE vip_cursor;
DEALLOCATE vip_cursor;
GO

-- Hàng K (SweetBox) - 15 ghế
DECLARE @j INT = 1;
WHILE @j <= 15
BEGIN
    DECLARE @mk NVARCHAR(5) = 'K' + CAST(@j AS NVARCHAR(3));
    IF NOT EXISTS (SELECT 1 FROM Ghe WHERE MaGhe = @mk)
    BEGIN
        INSERT INTO Ghe (MaGhe, Hang, SoGhe, Loai)
        VALUES (@mk, 'K', @j, N'SweetBox');
    END
    SET @j = @j + 1;
END
GO

-- 8) Đảm bảo TẤT CẢ ghế ở trạng thái 'Trong' (sẵn sàng chọn)
UPDATE Ghe
SET TrangThai = N'Trong';
GO

-- 9) Tạo index (nếu chưa có)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes idx
    JOIN sys.objects obj ON idx.object_id = obj.object_id
    WHERE idx.name = 'IX_Ghe_Hang_SoGhe' AND obj.name = 'Ghe'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Ghe_Hang_SoGhe ON Ghe (Hang, SoGhe);
END
GO

-- 10) Kiểm tra nhanh kết quả
SELECT COUNT(*) AS TotalLoai FROM LoaiGhe;
SELECT COUNT(*) AS TotalGhe FROM Ghe;
SELECT TrangThai, COUNT(*) AS SoLuong FROM Ghe GROUP BY TrangThai;
SELECT MaGhe, Hang, SoGhe, Loai, TrangThai FROM Ghe ORDER BY
    CASE WHEN Hang = 'K' THEN 'L' ELSE Hang END, SoGhe;
GO
