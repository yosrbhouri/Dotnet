using DeliveryApp.Data;
using DeliveryApp.Filters;
using DeliveryApp.Hubs;
using DeliveryApp.Models;
using DeliveryApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Database (SQL Server) ───────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ─── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ─── Cookie ──────────────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IColisService, ColisService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSignalR();
builder.Services.AddScoped<ProfilePictureFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ProfilePictureFilter>();
}).AddRazorRuntimeCompilation();

var app = builder.Build();

// ─── Seed Roles & Admin ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var context = services.GetRequiredService<AppDbContext>();

    context.Database.EnsureCreated();

    // Créer la table Notifications si elle n'existe pas (SQL Server)
    context.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications')
        BEGIN
            CREATE TABLE [Notifications] (
                [Id] INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_Notifications] PRIMARY KEY,
                [UserId] NVARCHAR(450) NOT NULL,
                [Title] NVARCHAR(MAX) NOT NULL,
                [Message] NVARCHAR(MAX) NOT NULL,
                [Type] INT NOT NULL DEFAULT 0,
                [Link] NVARCHAR(MAX) NULL,
                [IsRead] BIT NOT NULL DEFAULT 0,
                [CreatedAt] DATETIME2 NOT NULL
            );
            CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
        END
    ");

    // Ajouter les colonnes manquantes si elles n'existent pas (SQL Server)
    try { context.Database.ExecuteSqlRaw(@"IF COL_LENGTH('AspNetUsers','ProfilePicturePath') IS NULL ALTER TABLE [AspNetUsers] ADD [ProfilePicturePath] NVARCHAR(MAX) NULL;"); } catch { }
    try { context.Database.ExecuteSqlRaw(@"IF COL_LENGTH('AspNetUsers','TypeUtilisateur') IS NULL ALTER TABLE [AspNetUsers] ADD [TypeUtilisateur] INT NULL;"); } catch { }
    try { context.Database.ExecuteSqlRaw(@"IF COL_LENGTH('AspNetUsers','ClientId') IS NULL ALTER TABLE [AspNetUsers] ADD [ClientId] INT NULL;"); } catch { }
    try { context.Database.ExecuteSqlRaw(@"IF COL_LENGTH('AspNetUsers','LivreurId') IS NULL ALTER TABLE [AspNetUsers] ADD [LivreurId] INT NULL;"); } catch { }

    // Créer les rôles
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Créer l'admin par défaut
    if (await userManager.FindByEmailAsync("admin@delivery.tn") == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "admin@delivery.tn",
            Email = "admin@delivery.tn",
            FullName = "Administrateur Système",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    // Créer un utilisateur démo (même email qu'un client pour recevoir les notifications)
    if (await userManager.FindByEmailAsync("m.benali@email.tn") == null)
    {
        var demo = new ApplicationUser
        {
            UserName = "m.benali@email.tn",
            Email = "m.benali@email.tn",
            FullName = "Mohamed Ben Ali",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(demo, "User@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(demo, "User");

            // Quelques notifications de démo
            context.Notifications.AddRange(
                new DeliveryApp.Models.Notification { UserId = demo.Id, Title = "Bienvenue !", Message = "Votre compte a été créé. Vous recevrez ici les notifications de vos colis.", Type = DeliveryApp.Models.NotificationType.Success, CreatedAt = DateTime.Now.AddMinutes(-5) },
                new DeliveryApp.Models.Notification { UserId = demo.Id, Title = "Colis en préparation", Message = "Votre colis #1 est en attente de prise en charge.", Type = DeliveryApp.Models.NotificationType.Info, Link = "/Colis/Details/1", CreatedAt = DateTime.Now.AddHours(-1) }
            );
            await context.SaveChangesAsync();
        }
    }

    // Seed Données de démo
    if (!context.Clients.Any())
    {
        context.Clients.AddRange(
            new DeliveryApp.Models.Client { Nom="Ben Ali", Prenom="Mohamed", Ville="Tunis", CodePostal="1000", Email="m.benali@email.tn", Telephone="20123456" },
            new DeliveryApp.Models.Client { Nom="Trabelsi", Prenom="Sana", Ville="Sfax", CodePostal="3000", Email="s.trabelsi@email.tn", Telephone="25456789" },
            new DeliveryApp.Models.Client { Nom="Mansour", Prenom="Karim", Ville="Sousse", CodePostal="4000", Email="k.mansour@email.tn", Telephone="22789012" }
        );

        // Créer d'abord les livreurs (entreprises)
        var livreur1 = new DeliveryApp.Models.Livreur { CIN="12345678", RaisonSociale="Express Livraison SARL", Ville="Tunis", CodePostal="1000" };
        var livreur2 = new DeliveryApp.Models.Livreur { CIN="87654321", RaisonSociale="Speed Delivery", Ville="Sfax", CodePostal="3000" };
        context.Livreurs.AddRange(livreur1, livreur2);
        await context.SaveChangesAsync();

        // Créer les véhicules et les assigner à leurs entreprises
        var camion = new DeliveryApp.Models.Camion { Couleur="Blanc", Marque="Mercedes", Matricule="TN-123-AB", VitesseLimite=90, Capacite=5000, NbrEssieux=2, LivreurId=livreur1.Id };
        var voiture = new DeliveryApp.Models.Voiture { Couleur="Rouge", Marque="Peugeot", Matricule="TN-456-CD", VitesseLimite=130, NbrPlaces=5, LivreurId=livreur2.Id };
        context.Vehicules.AddRange(camion, voiture);
        await context.SaveChangesAsync();

        var livreurs = context.Livreurs.ToList();
        var clients = context.Clients.ToList();
        context.Colis.AddRange(
            new DeliveryApp.Models.Colis { DateLivraison=DateTime.Now.AddDays(2), Montant=45.5, Poids=1.2, Volume=0.02, Description="Documents importants", Statut=DeliveryApp.Models.StatutColis.EnAttente, ClientId=clients[0].Id, LivreurId=livreurs[0].Id },
            new DeliveryApp.Models.Colis { DateLivraison=DateTime.Now.AddDays(1), Montant=120.0, Poids=5.0, Volume=0.1, Description="Matériel informatique", Statut=DeliveryApp.Models.StatutColis.EnCours, ClientId=clients[1].Id, LivreurId=livreurs[0].Id },
            new DeliveryApp.Models.Colis { DateLivraison=DateTime.Now.AddDays(-1), Montant=30.0, Poids=0.5, Volume=0.01, Description="Vêtements", Statut=DeliveryApp.Models.StatutColis.Livré, ClientId=clients[2].Id, LivreurId=livreurs[1].Id },
            new DeliveryApp.Models.Colis { DateLivraison=DateTime.Now.AddDays(3), Montant=200.0, Poids=15.0, Volume=0.5, Description="Électroménager", Statut=DeliveryApp.Models.StatutColis.EnAttente, ClientId=clients[0].Id, LivreurId=livreurs[1].Id }
        );
        await context.SaveChangesAsync();
    }
}

// ─── Middleware ──────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
