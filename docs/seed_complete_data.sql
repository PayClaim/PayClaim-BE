/* =========================================================================================
   PAYCLAIM - COMPLETE UNIFORM SEED DATA SCRIPT (SSMS / SQL SERVER)
   =========================================================================================
   Password seluruh akun : Password123!
   BCrypt Hash           : $2a$11$fr0tuE/T.4dhphOP0CDewewjA/YRxo5wXrbs1gzj3w8RxMS21yeae

   Daftar Akun Utama:
   - admin               (Director HR - Admin)
   - andi.pratama        (Manager ENG - Manager, Employee)
   - dewi.lestari        (Manager FIN - Finance Manager, Finance, Manager, Employee)
   - hendra.wijaya       (Manager HR  - Manager, Employee)
   - surya.kencana       (Manager OPS - Manager, Employee)
   - fathan              (Staff L3 ENG - Employee, Manager: Andi Pratama)
   - abiem               (Staff L3 ENG - Employee, Manager: Andi Pratama)
   - ilham               (Senior L4 ENG - Employee, Manager: Andi Pratama)
   - budi.santoso        (Senior L4 ENG - Employee, Manager: Andi Pratama)
   + 44 akun karyawan lainnya di departemen ENG, FIN, HR, dan OPS
     (setiap departemen memiliki: 1 Manager, 5 Senior, 3 Staff, 2 Junior, 2 Intern).
   ========================================================================================= */

USE [PayClaim];
GO

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY

    -- =====================================================================================
    -- 0. HAPUS SELURUH DATA LAMA (URUTAN CHILD KE PARENT)
    -- =====================================================================================
    DELETE FROM [Claim_Approvals];
    DELETE FROM [Claims];
    DELETE FROM [Trip_approvals];
    DELETE FROM [Proposal_participants];
    DELETE FROM [Trip_proposals];
    DELETE FROM [Employee_Benefit];
    DELETE FROM [Benefit_Plans];
    DELETE FROM [Benefit];
    DELETE FROM [BenefitCategories];
    DELETE FROM [AccountRoles];
    DELETE FROM [Accounts];

    -- Lepaskan self-referencing FK sebelum hapus Employees & Departments
    UPDATE [Employees] SET [ManagerId] = NULL;
    DELETE FROM [Employees];

    DELETE FROM [Positions];

    UPDATE [Departments] SET [ParentDepartmentId] = NULL;
    DELETE FROM [Departments];

    DELETE FROM [Roles];

    -- Reset Identity Counter
    DBCC CHECKIDENT ('[BenefitCategories]', RESEED, 0);
    DBCC CHECKIDENT ('[Proposal_participants]', RESEED, 0);
    DBCC CHECKIDENT ('[Trip_approvals]', RESEED, 0);
    DBCC CHECKIDENT ('[Claim_Approvals]', RESEED, 0);

    -- =====================================================================================
    -- 1. SEED ROLES
    -- =====================================================================================
    DECLARE @RoleAdmin      UNIQUEIDENTIFIER = '00000001-0000-4000-8000-000000000001';
    DECLARE @RoleEmployee   UNIQUEIDENTIFIER = '00000002-0000-4000-8000-000000000002';
    DECLARE @RoleManager    UNIQUEIDENTIFIER = '00000003-0000-4000-8000-000000000003';
    DECLARE @RoleFinance    UNIQUEIDENTIFIER = '00000004-0000-4000-8000-000000000004';
    DECLARE @RoleFinManager UNIQUEIDENTIFIER = '00000005-0000-4000-8000-000000000005';

    INSERT INTO [Roles] ([Id], [Name], [Description]) VALUES
    (@RoleAdmin,      'Admin',           N'HR & System Administrator'),
    (@RoleEmployee,   'Employee',        N'General Employee'),
    (@RoleManager,    'Manager',         N'Department / Direct Manager'),
    (@RoleFinance,    'Finance',         N'Finance Officer'),
    (@RoleFinManager, 'Finance Manager', N'Finance Department Head');

    -- =====================================================================================
    -- 2. SEED DEPARTMENTS
    -- =====================================================================================
    INSERT INTO [Departments] ([Id], [Name], [ParentDepartmentId]) VALUES
    ('HQ',  N'Headquarter',            NULL),
    ('ENG', N'Engineering',            'HQ'),
    ('FIN', N'Finance & Accounting',   'HQ'),
    ('HR',  N'Human Resources',        'HQ'),
    ('OPS', N'Operations & Logistics', 'HQ');

    -- =====================================================================================
    -- 3. SEED POSITIONS
    -- =====================================================================================
    INSERT INTO [Positions] ([Id], [Title]) VALUES
    ('DIR', N'Director'),
    ('MGR', N'Manager'),
    ('L4',  N'Senior'),
    ('L3',  N'Staff'),
    ('L2',  N'Junior'),
    ('L1',  N'Intern');

    -- =====================================================================================
    -- 4. SEED EMPLOYEES (53 KARYAWAN: 1 DIR + 4 DEPT x [1 MGR, 5 L4, 3 L3, 2 L2, 2 L1])
    -- =====================================================================================
    DECLARE @PassHash VARCHAR(255) = '$2a$11$fr0tuE/T.4dhphOP0CDewewjA/YRxo5wXrbs1gzj3w8RxMS21yeae'; -- Password123!

    -- ID Managers & Key Employees
    DECLARE @EmpAdmin       UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000000'; -- Rina Wijaya (DIR - HR)
    DECLARE @MgrEng         UNIQUEIDENTIFIER = '20200000-88a1-4b11-9c01-000000000099'; -- Andi Pratama (MGR - ENG)
    DECLARE @MgrFin         UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000088'; -- Dewi Lestari (MGR - FIN)
    DECLARE @MgrHr          UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000077'; -- Hendra Wijaya (MGR - HR)
    DECLARE @MgrOps         UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000066'; -- Surya Kencana (MGR - OPS)

    -- ENG Employees (5 Senior L4, 3 Staff L3, 2 Junior L2, 2 Intern L1)
    DECLARE @EmpBudi        UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000001'; -- Budi Santoso (L4)
    DECLARE @EmpIlham       UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000002'; -- Ilham Saputra (L4)
    DECLARE @EmpEngL4_3     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000003'; -- Reza Rahadian (L4)
    DECLARE @EmpEngL4_4     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000004'; -- Dian Sastro (L4)
    DECLARE @EmpEngL4_5     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000005'; -- Arif Muhammad (L4)
    DECLARE @EmpFathan      UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000006'; -- Fathan Nuhi (L3)
    DECLARE @EmpAbiem       UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000007'; -- Abiem Pratama (L3)
    DECLARE @EmpEngL3_3     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000008'; -- Bagas Kara (L3)
    DECLARE @EmpEngL2_1     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000009'; -- Nadya Putri (L2)
    DECLARE @EmpEngL2_2     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000010'; -- Dimas Anggara (L2)
    DECLARE @EmpEngL1_1     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000011'; -- Kevin Sanjaya (L1)
    DECLARE @EmpEngL1_2     UNIQUEIDENTIFIER = '10100000-88a1-4b11-9c01-000000000012'; -- Putri Ayu (L1)

    -- FIN Employees (5 Senior L4, 3 Staff L3, 2 Junior L2, 2 Intern L1)
    DECLARE @EmpFinL4_1     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000001'; -- Tina Kartika (L4)
    DECLARE @EmpFinL4_2     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000002'; -- Sari Puspita (L4)
    DECLARE @EmpFinL4_3     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000003'; -- Hendri Gunawan (L4)
    DECLARE @EmpFinL4_4     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000004'; -- Maya Indah (L4)
    DECLARE @EmpFinL4_5     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000005'; -- Lukman Hakim (L4)
    DECLARE @EmpFinL3_1     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000006'; -- Riko Simanjuntak (L3)
    DECLARE @EmpFinL3_2     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000007'; -- Yuni Shara (L3)
    DECLARE @EmpFinL3_3     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000008'; -- Anton Wijaya (L3)
    DECLARE @EmpFinL2_1     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000009'; -- Bella Saphira (L2)
    DECLARE @EmpFinL2_2     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000010'; -- Cakra Khan (L2)
    DECLARE @EmpFinL1_1     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000011'; -- Dinda Hauw (L1)
    DECLARE @EmpFinL1_2     UNIQUEIDENTIFIER = '30300000-88a1-4b11-9c01-000000000012'; -- Edo Kondologit (L1)

    -- HR Employees (5 Senior L4, 3 Staff L3, 2 Junior L2, 2 Intern L1)
    DECLARE @EmpHrL4_1      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000001'; -- Sinta Nuriyah (L4)
    DECLARE @EmpHrL4_2      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000002'; -- Bayu Skak (L4)
    DECLARE @EmpHrL4_3      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000003'; -- Citra Kirana (L4)
    DECLARE @EmpHrL4_4      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000004'; -- Doni Salmanan (L4)
    DECLARE @EmpHrL4_5      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000005'; -- Eka Gustiwana (L4)
    DECLARE @EmpHrL3_1      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000006'; -- Fajar Sadboy (L3)
    DECLARE @EmpHrL3_2      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000007'; -- Gita Gutawa (L3)
    DECLARE @EmpHrL3_3      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000008'; -- Hadi Pranoto (L3)
    DECLARE @EmpHrL2_1      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000009'; -- Indah Permatasari (L2)
    DECLARE @EmpHrL2_2      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000010'; -- Joko Anwar (L2)
    DECLARE @EmpHrL1_1      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000011'; -- Kartika Putri (L1)
    DECLARE @EmpHrL1_2      UNIQUEIDENTIFIER = '40400000-88a1-4b11-9c01-000000000012'; -- Lutfi Agizal (L1)

    -- OPS Employees (5 Senior L4, 3 Staff L3, 2 Junior L2, 2 Intern L1)
    DECLARE @EmpOpsL4_1     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000001'; -- Mawar Eva (L4)
    DECLARE @EmpOpsL4_2     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000002'; -- Naufal Samudra (L4)
    DECLARE @EmpOpsL4_3     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000003'; -- Oki Setiana (L4)
    DECLARE @EmpOpsL4_4     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000004'; -- Pandji Pragiwaksono (L4)
    DECLARE @EmpOpsL4_5     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000005'; -- Qory Sandioriva (L4)
    DECLARE @EmpOpsL3_1     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000006'; -- Raffi Ahmad (L3)
    DECLARE @EmpOpsL3_2     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000007'; -- Syahrini Putri (L3)
    DECLARE @EmpOpsL3_3     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000008'; -- Taufik Hidayat (L3)
    DECLARE @EmpOpsL2_1     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000009'; -- Uus Komika (L2)
    DECLARE @EmpOpsL2_2     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000010'; -- Vina Panduwinata (L2)
    DECLARE @EmpOpsL1_1     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000011'; -- Wendi Cagur (L1)
    DECLARE @EmpOpsL1_2     UNIQUEIDENTIFIER = '50500000-88a1-4b11-9c01-000000000012'; -- Yasmine Wildblood (L1)

    -- Insert Director & Department Managers terlebih dahulu (ManagerId = NULL atau @EmpAdmin)
    INSERT INTO [Employees] ([Id], [Number], [FullName], [Email], [HireDate], [IsActive], [DepartmentId], [PositionId], [ManagerId]) VALUES
    (@EmpAdmin, 'EMP-2026-0001', N'Rina Wijaya',   N'admin@perusahaan.co.id',          '2020-01-01', 1, 'HR',  'DIR', NULL),
    (@MgrEng,   'EMP-2026-0099', N'Andi Pratama',  N'andi.pratama@perusahaan.co.id',   '2021-03-15', 1, 'ENG', 'MGR', @EmpAdmin),
    (@MgrFin,   'EMP-2026-0088', N'Dewi Lestari',  N'dewi.lestari@perusahaan.co.id',   '2021-02-10', 1, 'FIN', 'MGR', @EmpAdmin),
    (@MgrHr,    'EMP-2026-0077', N'Hendra Wijaya', N'hendra.wijaya@perusahaan.co.id',  '2021-06-01', 1, 'HR',  'MGR', @EmpAdmin),
    (@MgrOps,   'EMP-2026-0066', N'Surya Kencana', N'surya.kencana@perusahaan.co.id',  '2021-08-20', 1, 'OPS', 'MGR', @EmpAdmin);

    -- Insert ENG Employees (ManagerId = @MgrEng)
    INSERT INTO [Employees] ([Id], [Number], [FullName], [Email], [HireDate], [IsActive], [DepartmentId], [PositionId], [ManagerId]) VALUES
    (@EmpBudi,    'EMP-2026-0101', N'Budi Santoso',   N'budi.santoso@perusahaan.co.id',  '2022-01-10', 1, 'ENG', 'L4', @MgrEng),
    (@EmpIlham,   'EMP-2026-0102', N'Ilham Saputra',  N'ilham@perusahaan.co.id',         '2022-03-12', 1, 'ENG', 'L4', @MgrEng),
    (@EmpEngL4_3, 'EMP-2026-0103', N'Reza Rahadian',  N'reza.rahadian@perusahaan.co.id', '2022-05-18', 1, 'ENG', 'L4', @MgrEng),
    (@EmpEngL4_4, 'EMP-2026-0104', N'Dian Sastro',    N'dian.sastro@perusahaan.co.id',   '2022-07-22', 1, 'ENG', 'L4', @MgrEng),
    (@EmpEngL4_5, 'EMP-2026-0105', N'Arif Muhammad',  N'arif.muhammad@perusahaan.co.id', '2022-09-01', 1, 'ENG', 'L4', @MgrEng),
    (@EmpFathan,  'EMP-2026-0106', N'Fathan Nuhi',    N'fathan@perusahaan.co.id',        '2023-02-01', 1, 'ENG', 'L3', @MgrEng),
    (@EmpAbiem,   'EMP-2026-0107', N'Abiem Pratama',  N'abiem@perusahaan.co.id',         '2023-02-15', 1, 'ENG', 'L3', @MgrEng),
    (@EmpEngL3_3, 'EMP-2026-0108', N'Bagas Kara',     N'bagas.kara@perusahaan.co.id',    '2023-06-10', 1, 'ENG', 'L3', @MgrEng),
    (@EmpEngL2_1, 'EMP-2026-0109', N'Nadya Putri',    N'nadya.putri@perusahaan.co.id',   '2024-01-15', 1, 'ENG', 'L2', @MgrEng),
    (@EmpEngL2_2, 'EMP-2026-0110', N'Dimas Anggara',  N'dimas.anggara@perusahaan.co.id', '2024-03-01', 1, 'ENG', 'L2', @MgrEng),
    (@EmpEngL1_1, 'EMP-2026-0111', N'Kevin Sanjaya',  N'kevin.sanjaya@perusahaan.co.id', '2025-09-01', 1, 'ENG', 'L1', @MgrEng),
    (@EmpEngL1_2, 'EMP-2026-0112', N'Putri Ayu',      N'putri.ayu@perusahaan.co.id',     '2025-09-01', 1, 'ENG', 'L1', @MgrEng);

    -- Insert FIN Employees (ManagerId = @MgrFin)
    INSERT INTO [Employees] ([Id], [Number], [FullName], [Email], [HireDate], [IsActive], [DepartmentId], [PositionId], [ManagerId]) VALUES
    (@EmpFinL4_1, 'EMP-2026-0201', N'Tina Kartika',     N'tina.kartika@perusahaan.co.id',     '2022-02-01', 1, 'FIN', 'L4', @MgrFin),
    (@EmpFinL4_2, 'EMP-2026-0202', N'Sari Puspita',     N'sari.puspita@perusahaan.co.id',     '2022-04-10', 1, 'FIN', 'L4', @MgrFin),
    (@EmpFinL4_3, 'EMP-2026-0203', N'Hendri Gunawan',   N'hendri.gunawan@perusahaan.co.id',   '2022-06-15', 1, 'FIN', 'L4', @MgrFin),
    (@EmpFinL4_4, 'EMP-2026-0204', N'Maya Indah',       N'maya.indah@perusahaan.co.id',       '2022-08-20', 1, 'FIN', 'L4', @MgrFin),
    (@EmpFinL4_5, 'EMP-2026-0205', N'Lukman Hakim',     N'lukman.hakim@perusahaan.co.id',     '2022-11-05', 1, 'FIN', 'L4', @MgrFin),
    (@EmpFinL3_1, 'EMP-2026-0206', N'Riko Simanjuntak', N'riko.simanjuntak@perusahaan.co.id', '2023-03-12', 1, 'FIN', 'L3', @MgrFin),
    (@EmpFinL3_2, 'EMP-2026-0207', N'Yuni Shara',       N'yuni.shara@perusahaan.co.id',       '2023-05-19', 1, 'FIN', 'L3', @MgrFin),
    (@EmpFinL3_3, 'EMP-2026-0208', N'Anton Wijaya',     N'anton.wijaya@perusahaan.co.id',     '2023-08-01', 1, 'FIN', 'L3', @MgrFin),
    (@EmpFinL2_1, 'EMP-2026-0209', N'Bella Saphira',    N'bella.saphira@perusahaan.co.id',    '2024-02-10', 1, 'FIN', 'L2', @MgrFin),
    (@EmpFinL2_2, 'EMP-2026-0210', N'Cakra Khan',       N'cakra.khan@perusahaan.co.id',       '2024-04-22', 1, 'FIN', 'L2', @MgrFin),
    (@EmpFinL1_1, 'EMP-2026-0211', N'Dinda Hauw',       N'dinda.hauw@perusahaan.co.id',       '2025-10-01', 1, 'FIN', 'L1', @MgrFin),
    (@EmpFinL1_2, 'EMP-2026-0212', N'Edo Kondologit',   N'edo.kondologit@perusahaan.co.id',   '2025-10-01', 1, 'FIN', 'L1', @MgrFin);

    -- Insert HR Employees (ManagerId = @MgrHr)
    INSERT INTO [Employees] ([Id], [Number], [FullName], [Email], [HireDate], [IsActive], [DepartmentId], [PositionId], [ManagerId]) VALUES
    (@EmpHrL4_1, 'EMP-2026-0301', N'Sinta Nuriyah',     N'sinta.nuriyah@perusahaan.co.id',     '2022-01-20', 1, 'HR', 'L4', @MgrHr),
    (@EmpHrL4_2, 'EMP-2026-0302', N'Bayu Skak',         N'bayu.skak@perusahaan.co.id',         '2022-03-25', 1, 'HR', 'L4', @MgrHr),
    (@EmpHrL4_3, 'EMP-2026-0303', N'Citra Kirana',      N'citra.kirana@perusahaan.co.id',      '2022-05-30', 1, 'HR', 'L4', @MgrHr),
    (@EmpHrL4_4, 'EMP-2026-0304', N'Doni Salmanan',     N'doni.salmanan@perusahaan.co.id',     '2022-09-14', 1, 'HR', 'L4', @MgrHr),
    (@EmpHrL4_5, 'EMP-2026-0305', N'Eka Gustiwana',     N'eka.gustiwana@perusahaan.co.id',     '2022-12-01', 1, 'HR', 'L4', @MgrHr),
    (@EmpHrL3_1, 'EMP-2026-0306', N'Fajar Pratama',     N'fajar.pratama@perusahaan.co.id',     '2023-02-18', 1, 'HR', 'L3', @MgrHr),
    (@EmpHrL3_2, 'EMP-2026-0307', N'Gita Gutawa',       N'gita.gutawa@perusahaan.co.id',       '2023-06-25', 1, 'HR', 'L3', @MgrHr),
    (@EmpHrL3_3, 'EMP-2026-0308', N'Hadi Pranoto',      N'hadi.pranoto@perusahaan.co.id',      '2023-09-10', 1, 'HR', 'L3', @MgrHr),
    (@EmpHrL2_1, 'EMP-2026-0309', N'Indah Permatasari', N'indah.permatasari@perusahaan.co.id', '2024-01-20', 1, 'HR', 'L2', @MgrHr),
    (@EmpHrL2_2, 'EMP-2026-0310', N'Joko Anwar',        N'joko.anwar@perusahaan.co.id',        '2024-05-15', 1, 'HR', 'L2', @MgrHr),
    (@EmpHrL1_1, 'EMP-2026-0311', N'Kartika Putri',     N'kartika.putri@perusahaan.co.id',     '2025-08-15', 1, 'HR', 'L1', @MgrHr),
    (@EmpHrL1_2, 'EMP-2026-0312', N'Lutfi Agizal',      N'lutfi.agizal@perusahaan.co.id',      '2025-08-15', 1, 'HR', 'L1', @MgrHr);

    -- Insert OPS Employees (ManagerId = @MgrOps)
    INSERT INTO [Employees] ([Id], [Number], [FullName], [Email], [HireDate], [IsActive], [DepartmentId], [PositionId], [ManagerId]) VALUES
    (@EmpOpsL4_1, 'EMP-2026-0401', N'Mawar Eva',            N'mawar.eva@perusahaan.co.id',            '2022-02-14', 1, 'OPS', 'L4', @MgrOps),
    (@EmpOpsL4_2, 'EMP-2026-0402', N'Naufal Samudra',       N'naufal.samudra@perusahaan.co.id',       '2022-04-18', 1, 'OPS', 'L4', @MgrOps),
    (@EmpOpsL4_3, 'EMP-2026-0403', N'Oki Setiana',          N'oki.setiana@perusahaan.co.id',          '2022-07-07', 1, 'OPS', 'L4', @MgrOps),
    (@EmpOpsL4_4, 'EMP-2026-0404', N'Pandji Pragiwaksono',  N'pandji.pragiwaksono@perusahaan.co.id',  '2022-10-10', 1, 'OPS', 'L4', @MgrOps),
    (@EmpOpsL4_5, 'EMP-2026-0405', N'Qory Sandioriva',      N'qory.sandioriva@perusahaan.co.id',      '2022-12-12', 1, 'OPS', 'L4', @MgrOps),
    (@EmpOpsL3_1, 'EMP-2026-0406', N'Raffi Ahmad',          N'raffi.ahmad@perusahaan.co.id',          '2023-01-25', 1, 'OPS', 'L3', @MgrOps),
    (@EmpOpsL3_2, 'EMP-2026-0407', N'Syahrini Putri',       N'syahrini.putri@perusahaan.co.id',       '2023-04-30', 1, 'OPS', 'L3', @MgrOps),
    (@EmpOpsL3_3, 'EMP-2026-0408', N'Taufik Hidayat',       N'taufik.hidayat@perusahaan.co.id',       '2023-07-15', 1, 'OPS', 'L3', @MgrOps),
    (@EmpOpsL2_1, 'EMP-2026-0409', N'Uus Komika',           N'uus.komika@perusahaan.co.id',           '2024-03-10', 1, 'OPS', 'L2', @MgrOps),
    (@EmpOpsL2_2, 'EMP-2026-0410', N'Vina Panduwinata',     N'vina.panduwinata@perusahaan.co.id',     '2024-06-01', 1, 'OPS', 'L2', @MgrOps),
    (@EmpOpsL1_1, 'EMP-2026-0411', N'Wendi Cagur',          N'wendi.cagur@perusahaan.co.id',          '2025-09-15', 1, 'OPS', 'L1', @MgrOps),
    (@EmpOpsL1_2, 'EMP-2026-0412', N'Yasmine Wildblood',    N'yasmine.wildblood@perusahaan.co.id',    '2025-09-15', 1, 'OPS', 'L1', @MgrOps);

    -- =====================================================================================
    -- 5. SEED ACCOUNTS & ACCOUNT ROLES (PASSWORD: "Password123!")
    -- =====================================================================================
    INSERT INTO [Accounts] ([Id], [Username], [PasswordHash], [IsActive], [EmployeeId]) VALUES
    -- Admin & Managers
    ('10100000-1111-4a22-8b01-000000000000', 'admin',               @PassHash, 1, @EmpAdmin),
    ('20200000-1111-4a22-8b01-000000000099', 'andi.pratama',        @PassHash, 1, @MgrEng),
    ('30300000-1111-4a22-8b01-000000000088', 'dewi.lestari',        @PassHash, 1, @MgrFin),
    ('40400000-1111-4a22-8b01-000000000077', 'hendra.wijaya',       @PassHash, 1, @MgrHr),
    ('50500000-1111-4a22-8b01-000000000066', 'surya.kencana',       @PassHash, 1, @MgrOps),

    -- ENG Accounts
    ('10100000-1111-4a22-8b01-000000000001', 'budi.santoso',        @PassHash, 1, @EmpBudi),
    ('10100000-1111-4a22-8b01-000000000002', 'ilham',               @PassHash, 1, @EmpIlham),
    ('10100000-1111-4a22-8b01-000000000003', 'reza.rahadian',       @PassHash, 1, @EmpEngL4_3),
    ('10100000-1111-4a22-8b01-000000000004', 'dian.sastro',         @PassHash, 1, @EmpEngL4_4),
    ('10100000-1111-4a22-8b01-000000000005', 'arif.muhammad',       @PassHash, 1, @EmpEngL4_5),
    ('10100000-1111-4a22-8b01-000000000006', 'fathan',              @PassHash, 1, @EmpFathan),
    ('10100000-1111-4a22-8b01-000000000007', 'abiem',               @PassHash, 1, @EmpAbiem),
    ('10100000-1111-4a22-8b01-000000000008', 'bagas.kara',          @PassHash, 1, @EmpEngL3_3),
    ('10100000-1111-4a22-8b01-000000000009', 'nadya.putri',         @PassHash, 1, @EmpEngL2_1),
    ('10100000-1111-4a22-8b01-000000000010', 'dimas.anggara',       @PassHash, 1, @EmpEngL2_2),
    ('10100000-1111-4a22-8b01-000000000011', 'kevin.sanjaya',       @PassHash, 1, @EmpEngL1_1),
    ('10100000-1111-4a22-8b01-000000000012', 'putri.ayu',           @PassHash, 1, @EmpEngL1_2),

    -- FIN Accounts
    ('30300000-1111-4a22-8b01-000000000001', 'tina.kartika',        @PassHash, 1, @EmpFinL4_1),
    ('30300000-1111-4a22-8b01-000000000002', 'sari.puspita',        @PassHash, 1, @EmpFinL4_2),
    ('30300000-1111-4a22-8b01-000000000003', 'hendri.gunawan',      @PassHash, 1, @EmpFinL4_3),
    ('30300000-1111-4a22-8b01-000000000004', 'maya.indah',          @PassHash, 1, @EmpFinL4_4),
    ('30300000-1111-4a22-8b01-000000000005', 'lukman.hakim',        @PassHash, 1, @EmpFinL4_5),
    ('30300000-1111-4a22-8b01-000000000006', 'riko.simanjuntak',    @PassHash, 1, @EmpFinL3_1),
    ('30300000-1111-4a22-8b01-000000000007', 'yuni.shara',          @PassHash, 1, @EmpFinL3_2),
    ('30300000-1111-4a22-8b01-000000000008', 'anton.wijaya',        @PassHash, 1, @EmpFinL3_3),
    ('30300000-1111-4a22-8b01-000000000009', 'bella.saphira',       @PassHash, 1, @EmpFinL2_1),
    ('30300000-1111-4a22-8b01-000000000010', 'cakra.khan',          @PassHash, 1, @EmpFinL2_2),
    ('30300000-1111-4a22-8b01-000000000011', 'dinda.hauw',          @PassHash, 1, @EmpFinL1_1),
    ('30300000-1111-4a22-8b01-000000000012', 'edo.kondologit',      @PassHash, 1, @EmpFinL1_2),

    -- HR Accounts
    ('40400000-1111-4a22-8b01-000000000001', 'sinta.nuriyah',       @PassHash, 1, @EmpHrL4_1),
    ('40400000-1111-4a22-8b01-000000000002', 'bayu.skak',           @PassHash, 1, @EmpHrL4_2),
    ('40400000-1111-4a22-8b01-000000000003', 'citra.kirana',        @PassHash, 1, @EmpHrL4_3),
    ('40400000-1111-4a22-8b01-000000000004', 'doni.salmanan',       @PassHash, 1, @EmpHrL4_4),
    ('40400000-1111-4a22-8b01-000000000005', 'eka.gustiwana',       @PassHash, 1, @EmpHrL4_5),
    ('40400000-1111-4a22-8b01-000000000006', 'fajar.pratama',       @PassHash, 1, @EmpHrL3_1),
    ('40400000-1111-4a22-8b01-000000000007', 'gita.gutawa',         @PassHash, 1, @EmpHrL3_2),
    ('40400000-1111-4a22-8b01-000000000008', 'hadi.pranoto',        @PassHash, 1, @EmpHrL3_3),
    ('40400000-1111-4a22-8b01-000000000009', 'indah.permatasari',   @PassHash, 1, @EmpHrL2_1),
    ('40400000-1111-4a22-8b01-000000000010', 'joko.anwar',          @PassHash, 1, @EmpHrL2_2),
    ('40400000-1111-4a22-8b01-000000000011', 'kartika.putri',       @PassHash, 1, @EmpHrL1_1),
    ('40400000-1111-4a22-8b01-000000000012', 'lutfi.agizal',        @PassHash, 1, @EmpHrL1_2),

    -- OPS Accounts
    ('50500000-1111-4a22-8b01-000000000001', 'mawar.eva',           @PassHash, 1, @EmpOpsL4_1),
    ('50500000-1111-4a22-8b01-000000000002', 'naufal.samudra',      @PassHash, 1, @EmpOpsL4_2),
    ('50500000-1111-4a22-8b01-000000000003', 'oki.setiana',         @PassHash, 1, @EmpOpsL4_3),
    ('50500000-1111-4a22-8b01-000000000004', 'pandji.pragiwaksono', @PassHash, 1, @EmpOpsL4_4),
    ('50500000-1111-4a22-8b01-000000000005', 'qory.sandioriva',     @PassHash, 1, @EmpOpsL4_5),
    ('50500000-1111-4a22-8b01-000000000006', 'raffi.ahmad',         @PassHash, 1, @EmpOpsL3_1),
    ('50500000-1111-4a22-8b01-000000000007', 'syahrini.putri',      @PassHash, 1, @EmpOpsL3_2),
    ('50500000-1111-4a22-8b01-000000000008', 'taufik.hidayat',      @PassHash, 1, @EmpOpsL3_3),
    ('50500000-1111-4a22-8b01-000000000009', 'uus.komika',          @PassHash, 1, @EmpOpsL2_1),
    ('50500000-1111-4a22-8b01-000000000010', 'vina.panduwinata',    @PassHash, 1, @EmpOpsL2_2),
    ('50500000-1111-4a22-8b01-000000000011', 'wendi.cagur',         @PassHash, 1, @EmpOpsL1_1),
    ('50500000-1111-4a22-8b01-000000000012', 'yasmine.wildblood',   @PassHash, 1, @EmpOpsL1_2);

    -- Assign Role "Employee" ke seluruh karyawan non-Admin
    INSERT INTO [AccountRoles] ([RoleId], [AccountId], [AssignedAt])
    SELECT @RoleEmployee, [Id], '2026-01-01T08:00:00Z'
    FROM [Accounts]
    WHERE [Username] <> 'admin';

    -- Assign Role "Admin" ke admin
    INSERT INTO [AccountRoles] ([RoleId], [AccountId], [AssignedAt]) VALUES
    (@RoleAdmin, '10100000-1111-4a22-8b01-000000000000', '2026-01-01T08:00:00Z');

    -- Assign Role "Manager" ke seluruh Manager Departemen
    INSERT INTO [AccountRoles] ([RoleId], [AccountId], [AssignedAt]) VALUES
    (@RoleManager, '20200000-1111-4a22-8b01-000000000099', '2026-01-01T08:00:00Z'), -- Andi Pratama (ENG)
    (@RoleManager, '30300000-1111-4a22-8b01-000000000088', '2026-01-01T08:00:00Z'), -- Dewi Lestari (FIN)
    (@RoleManager, '40400000-1111-4a22-8b01-000000000077', '2026-01-01T08:00:00Z'), -- Hendra Wijaya (HR)
    (@RoleManager, '50500000-1111-4a22-8b01-000000000066', '2026-01-01T08:00:00Z'); -- Surya Kencana (OPS)

    -- Assign Role "Finance Manager" ke Dewi Lestari
    INSERT INTO [AccountRoles] ([RoleId], [AccountId], [AssignedAt]) VALUES
    (@RoleFinManager, '30300000-1111-4a22-8b01-000000000088', '2026-01-01T08:00:00Z');

    -- Assign Role "Finance" ke Dewi Lestari & anggota departemen FIN (L4 & L3)
    INSERT INTO [AccountRoles] ([RoleId], [AccountId], [AssignedAt])
    SELECT @RoleFinance, a.[Id], '2026-01-01T08:00:00Z'
    FROM [Accounts] a
    INNER JOIN [Employees] e ON a.[EmployeeId] = e.[Id]
    WHERE e.[DepartmentId] = 'FIN' AND e.[PositionId] IN ('MGR', 'L4', 'L3');

    -- =====================================================================================
    -- 6. SEED BENEFIT CATEGORIES & BENEFITS (Description Max 100 Karakter)
    --    PeriodType: 1 = Monthly, 2 = Yearly, 3 = OneTime
    -- =====================================================================================
    SET IDENTITY_INSERT [BenefitCategories] ON;
    INSERT INTO [BenefitCategories] ([Id], [Name]) VALUES
    (1, N'Kesehatan & Medis'),
    (2, N'Kesejahteraan & Kebugaran'),
    (3, N'Operasional & Kerja Jarak Jauh'),
    (4, N'Pengembangan Diri & Pendidikan');
    SET IDENTITY_INSERT [BenefitCategories] OFF;
    DBCC CHECKIDENT ('[BenefitCategories]', RESEED, 4);

    DECLARE @BenMedis      UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000001';
    DECLARE @BenOptik      UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000002';
    DECLARE @BenGym        UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000003';
    DECLARE @BenGigi       UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000004';
    DECLARE @BenMcu        UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000005';
    DECLARE @BenInternet   UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000006';
    DECLARE @BenKursus     UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000007';
    DECLARE @BenErgonomis  UNIQUEIDENTIFIER = 'b1010000-2222-4a11-8a01-000000000008';

    INSERT INTO [Benefit] ([Id], [Name], [Description], [PeriodType], [CategoryId]) VALUES
    (@BenMedis,     N'Tunjangan Rawat Jalan & Medis',    N'Plafon reimbursement konsultasi dokter, lab, dan obat resep.', 2, 1),
    (@BenOptik,     N'Tunjangan Kacamata & Optik',       N'Reimbursement frame dan lensa kacamata resep dokter mata.',     2, 1),
    (@BenGym,       N'Tunjangan Kebugaran & Gym',        N'Subsidi keanggotaan gym dan fasilitas olahraga bulanan.',       1, 2),
    (@BenGigi,      N'Tunjangan Perawatan Gigi',         N'Plafon perawatan gigi, scaling, tambal, dan tindakan medis.',   2, 1),
    (@BenMcu,       N'Medical Check Up Tahunan',         N'Paket pemeriksaan kesehatan menyeluruh (MCU) tahunan.',         2, 2),
    (@BenInternet,  N'Tunjangan Internet & Komunikasi',  N'Subsidi tagihan internet rumah dan paket data kerja bulanan.',  1, 3),
    (@BenKursus,    N'Tunjangan Sertifikasi & Kursus',   N'Bantuan biaya ujian sertifikasi profesional dan pelatihan.',    2, 4),
    (@BenErgonomis, N'Tunjangan Peralatan Kerja (WFH)',  N'Subsidi satu kali pembelian kursi atau meja kerja ergonomis.',  3, 3);

    -- =====================================================================================
    -- 7. SEED BENEFIT PLANS (SELURUH DEPARTEMEN & POSISI)
    -- =====================================================================================
    -- Helper CTE untuk generate Benefit Plans secara merata dan proporsional di semua Dept & Posisi
    INSERT INTO [Benefit_Plans] ([DepartmentId], [PositionId], [BenefitId], [LimitAmount])
    SELECT d.[Id], p.[Id], b.[BenefitId],
           CASE p.[Id]
               WHEN 'DIR' THEN b.[BaseLimit] * 2.50
               WHEN 'MGR' THEN b.[BaseLimit] * 1.75
               WHEN 'L4'  THEN b.[BaseLimit] * 1.25
               WHEN 'L3'  THEN b.[BaseLimit] * 1.00
               WHEN 'L2'  THEN b.[BaseLimit] * 0.75
               WHEN 'L1'  THEN b.[BaseLimit] * 0.40
           END AS [LimitAmount]
    FROM (VALUES ('ENG'), ('FIN'), ('HR'), ('OPS')) AS d([Id])
    CROSS JOIN (VALUES ('DIR'), ('MGR'), ('L4'), ('L3'), ('L2'), ('L1')) AS p([Id])
    CROSS JOIN (VALUES
        (@BenMedis,     8000000.00),
        (@BenOptik,     2000000.00),
        (@BenGym,        500000.00),
        (@BenGigi,      3000000.00),
        (@BenMcu,       1500000.00),
        (@BenInternet,   400000.00),
        (@BenKursus,    5000000.00),
        (@BenErgonomis, 2500000.00)
    ) AS b([BenefitId], [BaseLimit]);

    -- =====================================================================================
    -- 8. SEED EMPLOYEE BENEFITS (ALOKASI SALDO AKTIF UNTUK SELURUH KARYAWAN)
    -- =====================================================================================
    -- 8A. Insert dengan ID deterministik untuk karyawan utama agar mudah direferensikan di Claims
    DECLARE @EbBudiMedis     UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000001';
    DECLARE @EbBudiOptik     UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000002';
    DECLARE @EbBudiGym       UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000003';
    DECLARE @EbFathanMedis   UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000004';
    DECLARE @EbFathanOptik   UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000005';
    DECLARE @EbFathanGym     UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000006';
    DECLARE @EbFathanKursus  UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000007';
    DECLARE @EbAbiemMedis    UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000008';
    DECLARE @EbAbiemOptik    UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000009';
    DECLARE @EbAbiemInternet UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000010';
    DECLARE @EbIlhamMedis    UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000011';
    DECLARE @EbIlhamGigi     UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000012';
    DECLARE @EbIlhamKursus   UNIQUEIDENTIFIER = '0b010000-5555-4c44-9c01-000000000013';

    INSERT INTO [Employee_Benefit] ([Id], [EmployeeId], [BenefitId], [Balance], [StartDate], [EndDate]) VALUES
    -- Budi Santoso (L4 ENG)
    (@EbBudiMedis,     @EmpBudi,   @BenMedis,     8500000.00, '2026-01-01', '2026-12-31'),
    (@EbBudiOptik,     @EmpBudi,   @BenOptik,     1800000.00, '2026-01-01', '2026-12-31'),
    (@EbBudiGym,       @EmpBudi,   @BenGym,        625000.00, '2026-11-01', '2026-11-30'),
    -- Fathan Nuhi (L3 ENG)
    (@EbFathanMedis,   @EmpFathan, @BenMedis,     7250000.00, '2026-01-01', '2026-12-31'),
    (@EbFathanOptik,   @EmpFathan, @BenOptik,     2000000.00, '2026-01-01', '2026-12-31'),
    (@EbFathanGym,     @EmpFathan, @BenGym,        350000.00, '2026-11-01', '2026-11-30'),
    (@EbFathanKursus,  @EmpFathan, @BenKursus,    3500000.00, '2026-01-01', '2026-12-31'),
    -- Abiem Pratama (L3 ENG)
    (@EbAbiemMedis,    @EmpAbiem,  @BenMedis,     7600000.00, '2026-01-01', '2026-12-31'),
    (@EbAbiemOptik,    @EmpAbiem,  @BenOptik,     1250000.00, '2026-01-01', '2026-12-31'),
    (@EbAbiemInternet, @EmpAbiem,  @BenInternet,   400000.00, '2026-11-01', '2026-11-30'),
    -- Ilham Saputra (L4 ENG)
    (@EbIlhamMedis,    @EmpIlham,  @BenMedis,     9100000.00, '2026-01-01', '2026-12-31'),
    (@EbIlhamGigi,     @EmpIlham,  @BenGigi,      3200000.00, '2026-01-01', '2026-12-31'),
    (@EbIlhamKursus,   @EmpIlham,  @BenKursus,    4750000.00, '2026-01-01', '2026-12-31');

    -- 8B. Auto-alokasi sisa Benefit_Plans ke seluruh karyawan yang belum memiliki pasangan (EmployeeId, BenefitId) tersebut
    INSERT INTO [Employee_Benefit] ([Id], [EmployeeId], [BenefitId], [Balance], [StartDate], [EndDate])
    SELECT NEWID(), e.[Id], bp.[BenefitId], bp.[LimitAmount],
           CASE b.[PeriodType]
               WHEN 1 THEN '2026-11-01'
               ELSE '2026-01-01'
           END AS [StartDate],
           CASE b.[PeriodType]
               WHEN 1 THEN '2026-11-30'
               WHEN 2 THEN '2026-12-31'
               ELSE '2030-12-31'
           END AS [EndDate]
    FROM [Employees] e
    INNER JOIN [Benefit_Plans] bp
        ON e.[DepartmentId] = bp.[DepartmentId]
       AND e.[PositionId]   = bp.[PositionId]
    INNER JOIN [Benefit] b
        ON bp.[BenefitId] = b.[Id]
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Employee_Benefit] eb
        WHERE eb.[EmployeeId] = e.[Id]
          AND eb.[BenefitId]  = bp.[BenefitId]
    );

    -- =====================================================================================
    -- 9. SEED TRIP PROPOSALS, PARTICIPANTS & TRIP APPROVALS
    --    TripProposalStatus:
    --      1 = Submitted, 2 = UnderReview, 3 = NeedsRevision, 4 = Approved,
    --      5 = Rejected, 6 = Completed, 7 = Cancelled, 8 = FinanceManagerReview
    --    TripApprovalDecision:
    --      1 = Approved, 2 = Rejected, 3 = NeedsRevision, 4 = ForwardedToFinanceManager
    -- =====================================================================================
    DECLARE @Trip1  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000001'; -- Completed (Surabaya) - PIC: Fathan
    DECLARE @Trip2  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000002'; -- Approved (Bandung) - PIC: Ilham
    DECLARE @Trip3  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000003'; -- Approved (Bali) - PIC: Abiem
    DECLARE @Trip4  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000004'; -- FinanceManagerReview (Singapura) - PIC: Budi
    DECLARE @Trip5  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000005'; -- UnderReview (Yogyakarta) - PIC: Fathan
    DECLARE @Trip6  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000006'; -- Submitted (Semarang) - PIC: Abiem
    DECLARE @Trip7  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000007'; -- NeedsRevision (Medan) - PIC: Ilham
    DECLARE @Trip8  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000008'; -- Rejected (Tokyo) - PIC: Reza Rahadian
    DECLARE @Trip9  UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000009'; -- Completed (Jakarta Selatan) - PIC: Sinta Nuriyah (HR)
    DECLARE @Trip10 UNIQUEIDENTIFIER = '70700000-aaaa-4b11-9c01-000000000010'; -- UnderReview (Balikpapan) - PIC: Mawar Eva (OPS)

    INSERT INTO [Trip_proposals] ([Id], [Destination], [StartDate], [EndDate], [TotalAmount], [SubmittedAt], [Description], [Status], [FilePath]) VALUES
    (@Trip1,  N'Surabaya, Jawa Timur',       '2026-09-10', '2026-09-14', 12500000.00, '2026-09-01T09:15:00Z', N'Deployment sistem ERP cabang Surabaya dan UAT bersama tim operasional regional.', 6, '/uploads/reports/ed0c651c-eaee-48e6-8cb9-ae45747c691b.pdf'),
    (@Trip2,  N'Bandung, Jawa Barat',        '2026-11-18', '2026-11-21',  7500000.00, '2026-11-02T10:30:00Z', N'Workshop arsitektur microservices dan sprint planning Q1 bersama tim engineering.', 4, ''),
    (@Trip3,  N'Denpasar, Bali',             '2026-11-24', '2026-11-28', 18000000.00, '2026-11-03T14:00:00Z', N'Konferensi Tech Summit Asia 2026 dan presentasi produk PayClaim.',                 4, ''),
    (@Trip4,  N'Marina Bay, Singapura',      '2026-12-02', '2026-12-06', 32000000.00, '2026-11-05T11:20:00Z', N'Pertemuan kemitraan strategis cloud infrastructure regional APAC.',                8, ''),
    (@Trip5,  N'Yogyakarta, DIY',            '2026-12-10', '2026-12-13',  8500000.00, '2026-11-08T08:45:00Z', N'Kunjungan kampus rekrutmen engineer & kuliah tamu kemitraan universitas.',         2, ''),
    (@Trip6,  N'Semarang, Jawa Tengah',      '2026-12-15', '2026-12-18',  6400000.00, '2026-11-10T16:10:00Z', N'Audit infrastruktur jaringan kantor cabang Jawa Tengah.',                          1, ''),
    (@Trip7,  N'Medan, Sumatera Utara',      '2026-12-01', '2026-12-05', 14200000.00, '2026-11-06T13:00:00Z', N'Integrasi gateway pembayaran regional Sumatera Utara.',                            3, ''),
    (@Trip8,  N'Tokyo, Jepang',              '2026-12-20', '2026-12-27', 65000000.00, '2026-11-01T15:30:00Z', N'Kunjungan pameran AI internasional akhir tahun.',                                  5, ''),
    (@Trip9,  N'Jakarta Selatan, DKI',       '2026-10-05', '2026-10-07',  4500000.00, '2026-09-25T10:00:00Z', N'Job Fair Nasional & rekrutmen manajemen trainee batch 2026.',                     6, '/uploads/reports/ed0c651c-eaee-48e6-8cb9-ae45747c691b.pdf'),
    (@Trip10, N'Balikpapan, Kalimantan Timur','2026-12-08', '2026-12-12', 16500000.00, '2026-11-07T09:00:00Z', N'Inspeksi gudang logistik dan kalibrasi armada operasional Kalimantan.',            2, '');

    -- Proposal Participants
    INSERT INTO [Proposal_participants] ([TripProposalId], [EmployeeId], [IsPic], [Amount]) VALUES
    -- Trip 1: Surabaya (Total 12.500.000) -> Fathan (PIC), Abiem, Ilham
    (@Trip1, @EmpFathan,  1,  5000000.00),
    (@Trip1, @EmpAbiem,   0,  3750000.00),
    (@Trip1, @EmpIlham,   0,  3750000.00),
    -- Trip 2: Bandung (Total 7.500.000) -> Ilham (PIC), Fathan, Budi
    (@Trip2, @EmpIlham,   1,  2500000.00),
    (@Trip2, @EmpFathan,  0,  2500000.00),
    (@Trip2, @EmpBudi,    0,  2500000.00),
    -- Trip 3: Bali (Total 18.000.000) -> Abiem (PIC), Dian Sastro, Nadya Putri
    (@Trip3, @EmpAbiem,   1,  6500000.00),
    (@Trip3, @EmpEngL4_4, 0,  6500000.00),
    (@Trip3, @EmpEngL2_1, 0,  5000000.00),
    -- Trip 4: Singapura (Total 32.000.000) -> Budi (PIC), Ilham
    (@Trip4, @EmpBudi,    1, 16000000.00),
    (@Trip4, @EmpIlham,   0, 16000000.00),
    -- Trip 5: Yogyakarta (Total 8.500.000) -> Fathan (PIC), Bagas Kara, Kevin Sanjaya
    (@Trip5, @EmpFathan,  1,  3500000.00),
    (@Trip5, @EmpEngL3_3, 0,  3000000.00),
    (@Trip5, @EmpEngL1_1, 0,  2000000.00),
    -- Trip 6: Semarang (Total 6.400.000) -> Abiem (PIC), Dimas Anggara
    (@Trip6, @EmpAbiem,   1,  3400000.00),
    (@Trip6, @EmpEngL2_2, 0,  3000000.00),
    -- Trip 7: Medan (Total 14.200.000) -> Ilham (PIC), Arif Muhammad
    (@Trip7, @EmpIlham,   1,  7200000.00),
    (@Trip7, @EmpEngL4_5, 0,  7000000.00),
    -- Trip 8: Tokyo (Total 65.000.000) -> Reza Rahadian (PIC), Dian Sastro
    (@Trip8, @EmpEngL4_3, 1, 35000000.00),
    (@Trip8, @EmpEngL4_4, 0, 30000000.00),
    -- Trip 9: Jakarta Selatan (Total 4.500.000) -> Sinta Nuriyah (PIC), Fajar Pratama, Gita Gutawa
    (@Trip9, @EmpHrL4_1,  1,  1800000.00),
    (@Trip9, @EmpHrL3_1,  0,  1350000.00),
    (@Trip9, @EmpHrL3_2,  0,  1350000.00),
    -- Trip 10: Balikpapan (Total 16.500.000) -> Mawar Eva (PIC), Raffi Ahmad, Taufik Hidayat
    (@Trip10, @EmpOpsL4_1, 1, 6500000.00),
    (@Trip10, @EmpOpsL3_1, 0, 5000000.00),
    (@Trip10, @EmpOpsL3_3, 0, 5000000.00);

    -- Trip Approvals
    INSERT INTO [Trip_approvals] ([TripProposalId], [ApproverId], [ApproverName], [Decision], [Notes], [ApprovalLevel], [DecidedAt]) VALUES
    -- Trip 1 (Completed -> Approved L1 Manager, Forwarded L2 Finance, Approved L3 Finance Manager)
    (@Trip1, @MgrEng,     N'Andi Pratama',  1, N'Disetujui, proyek prioritas Q3.',                                   1, '2026-09-02T10:00:00Z'),
    (@Trip1, @EmpFinL4_1, N'Tina Kartika',  4, N'Anggaran sesuai plafon dinas, diteruskan ke Finance Manager.',      2, '2026-09-03T11:30:00Z'),
    (@Trip1, @MgrFin,     N'Dewi Lestari',  1, N'Dana perjalanan dinas Surabaya disetujui penuh.',                   3, '2026-09-04T09:00:00Z'),
    -- Trip 2 (Approved)
    (@Trip2, @MgrEng,     N'Andi Pratama',  1, N'Setuju, pastikan dokumentasi arsitektur selesai.',                  1, '2026-11-03T09:30:00Z'),
    (@Trip2, @EmpFinL4_1, N'Tina Kartika',  1, N'Biaya hotel dan transportasi kereta disetujui.',                    2, '2026-11-04T14:20:00Z'),
    -- Trip 3 (Approved)
    (@Trip3, @MgrEng,     N'Andi Pratama',  1, N'Disetujui untuk delegasi tim Engineering.',                         1, '2026-11-04T10:15:00Z'),
    (@Trip3, @EmpFinL4_2, N'Sari Puspita',  4, N'Nominal di atas Rp 15 juta diteruskan ke Finance Manager.',         2, '2026-11-05T13:00:00Z'),
    (@Trip3, @MgrFin,     N'Dewi Lestari',  1, N'Disetujui sebagai anggaran representasi perusahaan.',               3, '2026-11-06T08:45:00Z'),
    -- Trip 4 (FinanceManagerReview -> L1 Approved, L2 ForwardedToFinanceManager)
    (@Trip4, @MgrEng,     N'Andi Pratama',  1, N'Penting untuk negosiasi kontrak server tahun 2027.',                1, '2026-11-06T11:00:00Z'),
    (@Trip4, @EmpFinL4_1, N'Tina Kartika',  4, N'Dokumen lengkap, diteruskan ke Ibu Dewi untuk persetujuan akhir.', 2, '2026-11-07T15:40:00Z'),
    -- Trip 5 (UnderReview -> L1 Approved, menunggu Finance)
    (@Trip5, @MgrEng,     N'Andi Pratama',  1, N'Disetujui, koordinasikan jadwal dengan tim HR.',                    1, '2026-11-09T09:10:00Z'),
    -- Trip 7 (NeedsRevision -> diminta revisi oleh Manager)
    (@Trip7, @MgrEng,     N'Andi Pratama',  3, N'Mohon lampirkan rincian estimasi sewa kendaraan harian di Medan.',  1, '2026-11-07T10:05:00Z'),
    -- Trip 8 (Rejected -> ditolak Finance Manager karena efisiensi)
    (@Trip8, @MgrEng,     N'Andi Pratama',  1, N'Disetujui secara teknis.',                                          1, '2026-11-02T11:00:00Z'),
    (@Trip8, @MgrFin,     N'Dewi Lestari',  2, N'Ditolak, kuota perjalanan luar negeri Q4 sudah melampaui batas.',   2, '2026-11-03T16:30:00Z'),
    -- Trip 9 (Completed - HR)
    (@Trip9, @MgrHr,      N'Hendra Wijaya', 1, N'Disetujui untuk kebutuhan rekrutmen nasional.',                     1, '2026-09-26T09:00:00Z'),
    (@Trip9, @EmpFinL4_2, N'Sari Puspita',  1, N'Anggaran booth dan konsumsi tim disetujui.',                        2, '2026-09-27T13:15:00Z'),
    -- Trip 10 (UnderReview - OPS)
    (@Trip10, @MgrOps,    N'Surya Kencana', 1, N'Disetujui, prioritaskan gudang sektor utara Balikpapan.',           1, '2026-11-08T10:20:00Z');

    -- =====================================================================================
    -- 10. SEED CLAIMS & CLAIM APPROVALS
    --     ClaimType   : 1 = Benefit, 2 = OperationalReimbursement, 3 = BusinessTripReimbursement
    --     ClaimStatus : 1 = Submitted, 2 = UnderReview, 3 = NeedsRevision,
    --                   4 = Approved,  5 = Rejected,    6 = Paid
    --     ApprovalDecision: 1 = Approved, 2 = Rejected, 3 = NeedsRevision
    -- =====================================================================================
    DECLARE @Claim1  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000001'; -- Fathan: Benefit Medis (Paid)
    DECLARE @Claim2  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000002'; -- Fathan: Benefit Gym (UnderReview)
    DECLARE @Claim3  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000003'; -- Fathan: BusinessTrip Surabaya (Paid)
    DECLARE @Claim4  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000004'; -- Fathan: Operational Reimbursement (NeedsRevision)
    DECLARE @Claim5  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000005'; -- Fathan: Benefit Kursus AWS (Submitted)
    DECLARE @Claim6  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000006'; -- Abiem: Benefit Optik (Paid)
    DECLARE @Claim7  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000007'; -- Abiem: Benefit Medis (Approved)
    DECLARE @Claim8  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000008'; -- Abiem: BusinessTrip Bali (Submitted)
    DECLARE @Claim9  UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000009'; -- Abiem: Operational Reimbursement (Rejected)
    DECLARE @Claim10 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000010'; -- Ilham: Benefit Gigi (Paid)
    DECLARE @Claim11 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000011'; -- Ilham: BusinessTrip Bandung (UnderReview)
    DECLARE @Claim12 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000012'; -- Ilham: Benefit Kursus Kubernetes (Approved)
    DECLARE @Claim13 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000013'; -- Ilham: Operational Server Domain (NeedsRevision)
    DECLARE @Claim14 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000014'; -- Budi: Benefit Medis (Paid)
    DECLARE @Claim15 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000015'; -- Budi: Benefit Optik (UnderReview)
    DECLARE @Claim16 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000016'; -- Budi: Operational Lisensi JetBrains (Submitted)
    DECLARE @Claim17 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000017'; -- Tina Kartika (FIN): Operational Materai & Pajak (Approved)
    DECLARE @Claim18 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000018'; -- Sinta Nuriyah (HR): BusinessTrip Job Fair (Paid)
    DECLARE @Claim19 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000019'; -- Mawar Eva (OPS): Operational BBM Operasional (UnderReview)
    DECLARE @Claim20 UNIQUEIDENTIFIER = '90900000-cccc-4b11-9c01-000000000020'; -- Raffi Ahmad (OPS): Operational Sewa Forklift (Submitted)

    INSERT INTO [Claims] ([Id], [ClaimType], [Description], [TransactionDate], [Amount], [SubmittedAt], [Status], [FilePath], [EmployeeId], [EmployeeBenefitId], [TripProposalId]) VALUES
    -- Fathan Claims
    (@Claim1,  1, N'Konsultasi dokter spesialis penyakit dalam dan tebus resep obat di RS Pondok Indah.', '2026-10-12',  750000.00, '2026-10-13T09:20:00Z', 6, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpFathan, @EbFathanMedis,  NULL),
    (@Claim2,  1, N'Perpanjangan membership pusat kebugaran Celebrity Fitness bulan November 2026.',      '2026-11-02',  150000.00, '2026-11-03T10:15:00Z', 2, '/uploads/receipts/7661e839-51fc-404d-bdee-6fa4b10681e8.jpg', @EmpFathan, @EbFathanGym,    NULL),
    (@Claim3,  3, N'Reimbursement taksi bandara Juanda dan konsumsi tim selama UAT di Surabaya.',         '2026-09-14',  680000.00, '2026-09-16T14:00:00Z', 6, '/uploads/receipts/b2e6aac8-0e1f-4d17-943c-3645b958c63f.png', @EmpFathan, NULL,            @Trip1),
    (@Claim4,  2, N'Pembelian kabel HDMI 4K, USB-C hub, dan adaptor ruang meeting lantai 4.',            '2026-11-04',  425000.00, '2026-11-05T11:30:00Z', 3, '/uploads/receipts/7661e839-51fc-404d-bdee-6fa4b10681e8.jpg', @EmpFathan, NULL,            NULL),
    (@Claim5,  1, N'Biaya pendaftaran ujian sertifikasi AWS Certified Developer Associate.',              '2026-11-09', 1500000.00, '2026-11-10T08:50:00Z', 1, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpFathan, @EbFathanKursus, NULL),

    -- Abiem Claims
    (@Claim6,  1, N'Pembelian lensa kacamata anti-radiasi bluecut dan frame di Optik Melawai.',           '2026-10-18',  750000.00, '2026-10-19T13:10:00Z', 6, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpAbiem,  @EbAbiemOptik,   NULL),
    (@Claim7,  1, N'Pemeriksaan laboratorium darah lengkap dan vitamin di Prodia.',                       '2026-11-01',  400000.00, '2026-11-02T09:40:00Z', 4, '/uploads/receipts/b2e6aac8-0e1f-4d17-943c-3645b958c63f.png', @EmpAbiem,  @EbAbiemMedis,   NULL),
    (@Claim8,  3, N'Booking tiket pesawat PP Jakarta-Denpasar untuk Tech Summit Asia 2026.',              '2026-11-08', 2450000.00, '2026-11-09T15:25:00Z', 1, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpAbiem,  NULL,            @Trip3),
    (@Claim9,  2, N'Langganan akun ChatGPT Plus pribadi tanpa persetujuan kepala departemen.',            '2026-10-25',  330000.00, '2026-10-26T16:00:00Z', 5, '/uploads/receipts/7661e839-51fc-404d-bdee-6fa4b10681e8.jpg', @EmpAbiem,  NULL,            NULL),

    -- Ilham Claims
    (@Claim10, 1, N'Tindakan scaling karang gigi dan tambal sinar komposit di Klinik Gigi Senyum.',       '2026-10-08',  550000.00, '2026-10-09T10:00:00Z', 6, '/uploads/receipts/b2e6aac8-0e1f-4d17-943c-3645b958c63f.png', @EmpIlham,  @EbIlhamGigi,    NULL),
    (@Claim11, 3, N'Pembelian tiket kereta Whoosh Halim-Padalarang PP dan akomodasi tim Bandung.',        '2026-11-06', 1200000.00, '2026-11-07T09:15:00Z', 2, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpIlham,  NULL,            @Trip2),
    (@Claim12, 1, N'Pelatihan Certified Kubernetes Administrator (CKA) beserta voucher ujian resmi.',     '2026-11-01', 1500000.00, '2026-11-02T14:30:00Z', 4, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpIlham,  @EbIlhamKursus,  NULL),
    (@Claim13, 2, N'Perpanjangan sertifikat SSL Wildcard dan domain staging server internal.',            '2026-11-05',  890000.00, '2026-11-06T11:45:00Z', 3, '/uploads/receipts/7661e839-51fc-404d-bdee-6fa4b10681e8.jpg', @EmpIlham,  NULL,            NULL),

    -- Budi Santoso Claims
    (@Claim14, 1, N'Rawat jalan fisioterapi bahu dan obat pereda nyeri di RS Mitra Keluarga.',            '2026-10-15', 1500000.00, '2026-10-16T08:30:00Z', 6, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpBudi,   @EbBudiMedis,    NULL),
    (@Claim15, 1, N'Penggantian lensa kacamata silinder di Optik Seis.',                                  '2026-11-04',  700000.00, '2026-11-05T10:00:00Z', 2, '/uploads/receipts/b2e6aac8-0e1f-4d17-943c-3645b958c63f.png', @EmpBudi,   @EbBudiOptik,    NULL),
    (@Claim16, 2, N'Perpanjangan lisensi tahunan JetBrains All Products Pack untuk tim backend.',         '2026-11-09', 2350000.00, '2026-11-10T11:10:00Z', 1, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpBudi,   NULL,            NULL),

    -- Cross-Department Claims (FIN, HR, OPS)
    (@Claim17, 2, N'Pembelian e-Meterai 10.000 sebanyak 50 keping untuk dokumen kontrak vendor Q4.',      '2026-11-03',  525000.00, '2026-11-04T09:00:00Z', 4, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpFinL4_1, NULL,           NULL),
    (@Claim18, 3, N'Cetak brosur x-banner dan konsumsi panitia Job Fair Nasional Jakarta Selatan.',       '2026-10-07',  950000.00, '2026-10-08T13:20:00Z', 6, '/uploads/receipts/b2e6aac8-0e1f-4d17-943c-3645b958c63f.png', @EmpHrL4_1,  NULL,           @Trip9),
    (@Claim19, 2, N'Pengisian BBM Dexlite dan tol armada operasional pengiriman gudang Cikarang.',        '2026-11-06',  640000.00, '2026-11-07T08:40:00Z', 2, '/uploads/receipts/7661e839-51fc-404d-bdee-6fa4b10681e8.jpg', @EmpOpsL4_1, NULL,           NULL),
    (@Claim20, 2, N'Biaya perawatan berkala dan ganti oli forklift gudang logistik blok B.',              '2026-11-09', 1450000.00, '2026-11-10T14:15:00Z', 1, '/uploads/receipts/28828621-7791-4d01-8e5e-bbe9510e60db.pdf', @EmpOpsL3_1, NULL,           NULL);

    -- Claim Approvals
    INSERT INTO [Claim_Approvals] ([ClaimId], [ApproverId], [ApproverName], [Decision], [Notes], [ApprovalLevel], [DecidedAt]) VALUES
    -- Claim 1 (Fathan - Paid)
    (@Claim1,  @MgrEng,     N'Andi Pratama',  1, N'Kwitansi rumah sakit dan resep dokter lengkap.',                      1, '2026-10-14T10:00:00Z'),
    (@Claim1,  @EmpFinL4_1, N'Tina Kartika',  1, N'Verifikasi plafon medis sesuai, dana telah dicairkan.',               2, '2026-10-15T14:30:00Z'),
    -- Claim 2 (Fathan - UnderReview -> L1 Approved)
    (@Claim2,  @MgrEng,     N'Andi Pratama',  1, N'Disetujui, bukti bayar membership valid.',                            1, '2026-11-04T09:15:00Z'),
    -- Claim 3 (Fathan - Paid)
    (@Claim3,  @MgrEng,     N'Andi Pratama',  1, N'Sesuai penugasan dinas Surabaya.',                                    1, '2026-09-17T11:00:00Z'),
    (@Claim3,  @EmpFinL4_2, N'Sari Puspita',  1, N'Reimbursement perjalanan dinas telah ditransfer.',                    2, '2026-09-18T15:20:00Z'),
    -- Claim 4 (Fathan - NeedsRevision)
    (@Claim4,  @MgrEng,     N'Andi Pratama',  3, N'Foto struk agak buram pada bagian nominal PPN, mohon unggah ulang.',  1, '2026-11-06T10:40:00Z'),
    -- Claim 6 (Abiem - Paid)
    (@Claim6,  @MgrEng,     N'Andi Pratama',  1, N'Disetujui sesuai plafon optik tahunan.',                              1, '2026-10-20T09:30:00Z'),
    (@Claim6,  @EmpFinL4_1, N'Tina Kartika',  1, N'Pembayaran klaim kacamata selesai diproses.',                         2, '2026-10-21T13:45:00Z'),
    -- Claim 7 (Abiem - Approved -> siap dibayar Finance)
    (@Claim7,  @MgrEng,     N'Andi Pratama',  1, N'Disetujui.',                                                          1, '2026-11-03T08:50:00Z'),
    (@Claim7,  @EmpFinL4_2, N'Sari Puspita',  1, N'Dokumen valid, dijadwalkan pada batch pencairan minggu ini.',         2, '2026-11-04T11:10:00Z'),
    -- Claim 9 (Abiem - Rejected)
    (@Claim9,  @MgrEng,     N'Andi Pratama',  2, N'Ditolak, gunakan akun AI Enterprise resmi yang disediakan kantor.',   1, '2026-10-27T09:00:00Z'),
    -- Claim 10 (Ilham - Paid)
    (@Claim10, @MgrEng,     N'Andi Pratama',  1, N'Disetujui.',                                                          1, '2026-10-10T10:20:00Z'),
    (@Claim10, @EmpFinL4_1, N'Tina Kartika',  1, N'Telah dibayarkan ke rekening karyawan.',                              2, '2026-10-11T14:00:00Z'),
    -- Claim 11 (Ilham - UnderReview -> L1 Approved)
    (@Claim11, @MgrEng,     N'Andi Pratama',  1, N'Tiket dan invoice hotel sesuai dengan peserta trip Bandung.',         1, '2026-11-08T09:30:00Z'),
    -- Claim 12 (Ilham - Approved)
    (@Claim12, @MgrEng,     N'Andi Pratama',  1, N'Sangat mendukung kebutuhan infrastruktur cluster tahun ini.',         1, '2026-11-03T10:15:00Z'),
    (@Claim12, @EmpFinL4_2, N'Sari Puspita',  1, N'Disetujui oleh Finance, menunggu jadwal transfer.',                   2, '2026-11-04T16:00:00Z'),
    -- Claim 13 (Ilham - NeedsRevision)
    (@Claim13, @MgrEng,     N'Andi Pratama',  1, N'Disetujui secara teknis.',                                            1, '2026-11-07T09:00:00Z'),
    (@Claim13, @EmpFinL4_1, N'Tina Kartika',  3, N'Mohon lampirkan invoice resmi ber-NPWP dari registrar domain.',       2, '2026-11-08T13:30:00Z'),
    -- Claim 14 (Budi - Paid)
    (@Claim14, @MgrEng,     N'Andi Pratama',  1, N'Disetujui, semoga lekas pulih.',                                      1, '2026-10-17T09:10:00Z'),
    (@Claim14, @EmpFinL4_1, N'Tina Kartika',  1, N'Pencairan klaim medis selesai.',                                      2, '2026-10-18T11:25:00Z'),
    -- Claim 15 (Budi - UnderReview)
    (@Claim15, @MgrEng,     N'Andi Pratama',  1, N'Disetujui.',                                                          1, '2026-11-06T08:45:00Z'),
    -- Claim 17 (Tina Kartika FIN - Approved)
    (@Claim17, @MgrFin,     N'Dewi Lestari',  1, N'Disetujui untuk kebutuhan legal dan administrasi vendor.',            1, '2026-11-05T09:30:00Z'),
    (@Claim17, @EmpFinL4_2, N'Sari Puspita',  1, N'Diverifikasi oleh tim Finance.',                                      2, '2026-11-05T14:00:00Z'),
    -- Claim 18 (Sinta Nuriyah HR - Paid)
    (@Claim18, @MgrHr,      N'Hendra Wijaya', 1, N'Disetujui sesuai realisasi kegiatan Job Fair.',                       1, '2026-10-09T10:00:00Z'),
    (@Claim18, @EmpFinL4_1, N'Tina Kartika',  1, N'Dana reimbursement telah ditransfer.',                                2, '2026-10-10T15:10:00Z'),
    -- Claim 19 (Mawar Eva OPS - UnderReview)
    (@Claim19, @MgrOps,     N'Surya Kencana', 1, N'Struk BBM dan e-Toll sesuai rute pengiriman Cikarang.',               1, '2026-11-08T11:00:00Z');

    COMMIT TRANSACTION;
    PRINT '========================================================================';
    PRINT 'SEED DATA PAYCLAIM BERHASIL DIEKSEKUSI DENGAN LENGKAP!';
    PRINT 'Total Roles                : 5';
    PRINT 'Total Departments          : 5 (HQ, ENG, FIN, HR, OPS)';
    PRINT 'Total Positions            : 6 (DIR, MGR, L4, L3, L2, L1)';
    PRINT 'Total Employees & Accounts : 53 (Password semua akun: Password123!)';
    PRINT 'Total Benefit Categories   : 4';
    PRINT 'Total Benefits             : 8';
    PRINT 'Total Benefit Plans        : 192 (4 Dept x 6 Posisi x 8 Benefit)';
    PRINT 'Total Trip Proposals       : 10 (+ 26 Participants & 17 Approvals)';
    PRINT 'Total Claims               : 20 (+ 26 Approvals)';
    PRINT '========================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine    INT            = ERROR_LINE();
    RAISERROR ('Gagal mengeksekusi seed script pada baris %d: %s', 16, 1, @ErrorLine, @ErrorMessage);
END CATCH;
GO
