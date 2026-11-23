-- 1. Categories
UPDATE c
SET c.CategoryNameAr = v.ArabicName
FROM dbo.Categories c
JOIN (VALUES
    (N'IT Equipment',         N'معدات تقنية المعلومات'),
    (N'Office Furniture',     N'أثاث مكتبي'),
    (N'Vehicles',             N'المركبات'),
    (N'Peripherals',           N'الملحقات'),
    (N'Production Facility',  N'منشأة الإنتاج'),
    (N'Maintenance Facility', N'منشأة الصيانة'),
    (N'Administrative',       N'إداري')
) v(EnglishName, ArabicName)
    ON v.EnglishName = c.CategoryName
WHERE c.CategoryNameAr IS NULL;


-- 2. Asset Types
UPDATE t
SET t.TypeNameAr = v.ArabicName
FROM dbo.AssetTypes t
JOIN (VALUES
    (N'Laptop',          N'لابتوب'),
    (N'Desktop',         N'كمبيوتر مكتبي'),
    (N'Monitor',         N'شاشة'),
    (N'Printer',         N'طابعة'),
    (N'Mobile Phone',    N'هاتف محمول'),
    (N'Docking Station', N'محطة توصيل'),
    (N'Desk',            N'مكتب'),
    (N'Chair',           N'كرسي'),
    (N'Van',             N'سيارة فان')
) v(EnglishName, ArabicName)
    ON v.EnglishName = t.TypeName
WHERE t.TypeNameAr IS NULL;


-- 3. Departments
UPDATE d
SET d.DepartmentNameAr = v.ArabicName
FROM dbo.Departments d
JOIN (VALUES
    (N'Information Technology', N'تقنية المعلومات'),
    (N'Presales',                N'ما قبل البيع'),
    (N'Finance',                 N'المالية'),
    (N'Human Resources',         N'الموارد البشرية'),
    (N'Operations',              N'العمليات'),
    (N'Software',                N'البرمجيات'),
    (N'Hardware',                N'الأجهزة'),
    (N'Quality Assurance',       N'ضمان الجودة'),
    (N'IT Support',              N'الدعم الفني')
) v(EnglishName, ArabicName)
    ON v.EnglishName = d.DepartmentName
WHERE d.DepartmentNameAr IS NULL;


-- 4. Locations
UPDATE l
SET l.LocationNameAr = v.ArabicName
FROM dbo.Locations l
JOIN (VALUES
    (N'HQ - Floor 1',             N'المقر الرئيسي - الدور الأول'),
    (N'HQ - Floor 2',             N'المقر الرئيسي - الدور الثاني'),
    (N'HQ - Server Room',         N'المقر الرئيسي - غرفة الخوادم'),
    (N'Alexandria Warehouse',     N'مخزن الإسكندرية'),
    (N'Alexandria Warehouse 2',   N'مخزن الإسكندرية 2'),
    (N'Giza Distribution Center', N'مركز توزيع الجيزة'),
    (N'Remote / Work From Home',  N'عن بُعد / العمل من المنزل')
) v(EnglishName, ArabicName)
    ON v.EnglishName = l.LocationName
WHERE l.LocationNameAr IS NULL;

GO
