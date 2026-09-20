using Microsoft.AspNetCore.Authorization;
using DeliveryApp.Data;
using DeliveryApp.Models;
using DeliveryApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Validation specifique selon le type
            if (model.TypeUtilisateur == UserType.Client)
            {
                if (string.IsNullOrWhiteSpace(model.Ville))
                    ModelState.AddModelError("Ville", "La ville est obligatoire.");
                if (string.IsNullOrWhiteSpace(model.CodePostal))
                    ModelState.AddModelError("CodePostal", "Le code postal est obligatoire.");
            }
            else if (model.TypeUtilisateur == UserType.Livreur)
            {
                if (string.IsNullOrWhiteSpace(model.CIN))
                    ModelState.AddModelError("CIN", "Le CIN est obligatoire.");
                else if (_context.Livreurs.Any(l => l.CIN == model.CIN))
                    ModelState.AddModelError("CIN", "Ce CIN est deja utilise par un autre livreur.");
                if (string.IsNullOrWhiteSpace(model.RaisonSociale))
                    ModelState.AddModelError("RaisonSociale", "La raison sociale est obligatoire.");
                if (string.IsNullOrWhiteSpace(model.Ville))
                    ModelState.AddModelError("Ville", "La ville est obligatoire.");
                if (string.IsNullOrWhiteSpace(model.CodePostal))
                    ModelState.AddModelError("CodePostal", "Le code postal est obligatoire.");
            }

            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                TypeUtilisateur = model.TypeUtilisateur
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Creer l'entite Client ou Livreur correspondante
                if (model.TypeUtilisateur == UserType.Client)
                {
                    var nameParts = model.FullName.Split(' ', 2);
                    var client = new Client
                    {
                        Nom = nameParts.Length > 1 ? nameParts[1] : nameParts[0],
                        Prenom = nameParts[0],
                        Email = model.Email,
                        Telephone = model.Telephone ?? "",
                        Ville = model.Ville!,
                        CodePostal = model.CodePostal!
                    };
                    _context.Clients.Add(client);
                    await _context.SaveChangesAsync();

                    user.ClientId = client.Id;
                    await _userManager.UpdateAsync(user);
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else if (model.TypeUtilisateur == UserType.Livreur)
                {
                    var livreur = new Livreur
                    {
                        CIN = model.CIN!,
                        RaisonSociale = model.RaisonSociale!,
                        Ville = model.Ville!,
                        CodePostal = model.CodePostal!
                    };
                    _context.Livreurs.Add(livreur);
                    await _context.SaveChangesAsync();

                    user.LivreurId = livreur.Id;
                    await _userManager.UpdateAsync(user);
                    await _userManager.AddToRoleAsync(user, "User");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        private IActionResult? RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return null;
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // --- Profil utilisateur ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(IFormFile? profilePicture, string? fullName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (!string.IsNullOrWhiteSpace(fullName))
                user.FullName = fullName;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Format non supporte. Utilisez JPG ou PNG uniquement.";
                    return View(user);
                }

                if (profilePicture.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "L'image ne doit pas depasser 2 Mo.";
                    return View(user);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName = $"{user.Id}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
            }

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profil mis a jour avec succes !";
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (!string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);

                user.ProfilePicturePath = null;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "Photo de profil supprimee.";
            return RedirectToAction("Profile");
        }
    }
}
