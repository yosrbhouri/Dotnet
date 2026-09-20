# 🚚 DeliveryApp — Application Web de Gestion des Livraisons

Application ASP.NET Core MVC 8 complète pour la gestion des livraisons en ligne.

---

## 📋 Technologies Utilisées

| Composant        | Technologie                          |
|-----------------|--------------------------------------|
| Framework        | ASP.NET Core MVC 8                   |
| ORM             | Entity Framework Core 8              |
| Base de données  | SQLite (configurable SQL Server)     |
| Authentification | ASP.NET Core Identity                |
| Patron de conception | Repository Pattern, Service Layer |
| Frontend        | Razor Views + CSS custom + Chart.js  |

---

## 🏗️ Architecture du Projet

```
DeliveryApp/
├── Models/                  # Entités métier
│   ├── Vehicule.cs          # Classe abstraite (TPH)
│   ├── Camion.cs            # Héritage Vehicule
│   ├── Voiture.cs           # Héritage Vehicule
│   ├── Client.cs            # Entité Client
│   ├── Livreur.cs           # Entité Livreur
│   ├── Colis.cs             # Entité Colis (avec enum StatutColis)
│   └── ApplicationUser.cs   # Extension IdentityUser
│
├── Data/
│   └── AppDbContext.cs      # DbContext + Fluent API complet
│
├── Services/                # Couche Service (patron)
│   ├── IRepository.cs       # Interface générique Repository
│   ├── Repository.cs        # Implémentation générique
│   ├── IColisService.cs     # Interface service colis
│   └── ColisService.cs      # Logique métier colis
│
├── Controllers/
│   ├── AccountController.cs # Login, Register, Logout
│   ├── HomeController.cs    # Dashboard utilisateur
│   ├── ColisController.cs   # CRUD Colis + Recherche
│   ├── AdminController.cs   # Panneau admin complet
│   └── MicroservicesController.cs  # API REST
│
├── ViewModels/
│   └── ViewModels.cs        # LoginVM, RegisterVM, DashboardVM, SearchVM
│
└── Views/
    ├── Account/             # Login + Register
    ├── Home/                # Dashboard
    ├── Colis/               # Index, Create, Edit, Details, Search
    ├── Admin/               # Dashboard admin + CRUD toutes entités
    └── Shared/              # _Layout, _AuthLayout
```

---

## 🎯 Patrons de Conception Implémentés

### 1. Repository Pattern (Générique)
```csharp
// Interface générique
IRepository<T> where T : class

// Utilisation
var repo = new Repository<Client>(context);
var clients = await repo.GetAllAsync();
var found = await repo.FindAsync(c => c.Ville == "Tunis");
```

### 2. Service Layer Pattern
```csharp
// Séparation logique métier dans IColisService / ColisService
// Injection de dépendances via DI Container
builder.Services.AddScoped<IColisService, ColisService>();
```

### 3. Dependency Injection (IoC)
```csharp
// Enregistrement dans Program.cs
builder.Services.AddScoped<IColisService, ColisService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

### 4. TPH (Table Per Hierarchy) — Héritage
```csharp
// Vehicule → Camion / Voiture
// Une seule table "Vehicules" avec colonne discriminante "TypeVehicule"
modelBuilder.Entity<Vehicule>()
    .HasDiscriminator<string>("TypeVehicule")
    .HasValue<Camion>("Camion")
    .HasValue<Voiture>("Voiture");
```

---

## 🗃️ Diagramme de Classes

```
Vehicule (abstract)
├── Couleur, Marque, Matricule, VitesseLimite
├── Camion : Vehicule
│   ├── Capacite, NbrEssieux
└── Voiture : Vehicule
    └── NbrPlaces

Client
└── Id, CodePostal, Nom, Prenom, Ville, Email, Telephone
    └── ICollection<Colis>

Livreur
└── Id, CIN, CodePostal, RaisonSociale, Ville
    ├── VehiculeId → Vehicule
    └── ICollection<Colis>

Colis
└── Id, DateLivraison, Montant, Poids, Volume, Description, Statut
    ├── ClientId → Client
    └── LivreurId → Livreur

ApplicationUser (IdentityUser)
└── FullName, DateInscription, IsActive
```

---

## 🔐 Authentification & Rôles

| Rôle  | Accès                                               |
|-------|-----------------------------------------------------|
| Admin | CRUD complet, statistiques, gestion utilisateurs     |
| User  | Consultation liste colis, recherche, détails         |

**Compte admin par défaut :**
- Email : `admin@delivery.tn`
- Mot de passe : `Admin@123`

---

## 🔍 Fonctionnalités

### Utilisateur (User)
- ✅ Connexion / Inscription
- ✅ Tableau de bord avec statistiques personnelles
- ✅ Liste des colis avec statuts colorés
- ✅ Recherche avancée (description, montant min/max, statut)
- ✅ Vue détaillée d'un colis avec livreur et véhicule

### Administrateur (Admin)
- ✅ Tout ce que l'utilisateur peut faire
- ✅ CRUD Colis (Créer, Lire, Modifier, Supprimer)
- ✅ CRUD Clients
- ✅ CRUD Livreurs (avec assignation véhicule)
- ✅ CRUD Véhicules (Camion / Voiture)
- ✅ Gestion des utilisateurs (activer/désactiver)
- ✅ Statistiques avec graphiques (Chart.js)
- ✅ Changement de statut des colis

---

## 🌐 API REST (Microservices)

| Méthode | Endpoint                         | Description              | Rôle  |
|---------|----------------------------------|--------------------------|-------|
| GET     | `/api/colisapi`                  | Liste tous les colis     | All   |
| GET     | `/api/colisapi/{id}`             | Détail d'un colis        | All   |
| GET     | `/api/colisapi/stats`            | Statistiques             | All   |
| POST    | `/api/colisapi`                  | Créer un colis           | Admin |
| PUT     | `/api/colisapi/{id}`             | Modifier un colis        | Admin |
| DELETE  | `/api/colisapi/{id}`             | Supprimer un colis       | Admin |
| PATCH   | `/api/colisapi/{id}/statut`      | Changer statut           | Admin |
| GET     | `/api/clientsapi`                | Liste clients            | All   |
| GET     | `/api/clientsapi/{id}`           | Détail client            | All   |

---

## ▶️ Installation & Lancement

```bash
# 1. Restaurer les packages
dotnet restore

# 2. Créer la migration initiale
dotnet ef migrations add InitialCreate

# 3. Appliquer la migration
dotnet ef database update

# 4. Lancer l'application
dotnet run

# L'application sera disponible sur :
# https://localhost:5001 ou http://localhost:5000
```

---

## 📊 Annotations & Contraintes Utilisées

### Data Annotations
```csharp
[Required(ErrorMessage = "...")]
[StringLength(100, MinimumLength = 2)]
[Range(0.01, 100000)]
[EmailAddress]
[Phone]
[DataType(DataType.Currency)]
[RegularExpression(@"...")]
```

### Fluent API
```csharp
entity.Property(c => c.Montant).HasColumnType("decimal(18,2)");
entity.HasIndex(l => l.CIN).IsUnique();
entity.HasOne(c => c.Client).WithMany(cl => cl.Colis)
      .HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.Restrict);
```

---

## 🎨 Design

- **Couleurs** : Navy profond `#0B1D3A` + Orange électrique `#FF6B35`
- **Typographie** : Syne (titres) + DM Sans (corps)
- **Composants** : Cards, badges statuts, tables responsives, sidebar fixe
- **Graphiques** : Chart.js (donut + barres)
- **Responsive** : Mobile-first avec sidebar rétractable

---

*Mini Projet — Atelier Développement .NET*
