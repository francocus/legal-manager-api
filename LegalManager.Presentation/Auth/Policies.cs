using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;
using LegalManager.Application.Auth.Interfaces;
using LegalManager.Application.Users.Interfaces;
using LegalManager.Application.Cases.Interfaces;
using LegalManager.Application.Appointments.Interfaces;
using LegalManager.Application.Documents.Interfaces;
using LegalManager.Application.Common.Interfaces;

namespace LegalManager.Presentation.Auth
{
    public static class Policies
    {
        public const string AdminsOnly = "AdminsOnly";

        public const string AdminOrLawyer = "AdminOrLawyer";

        public const string AllRoles = "AllRoles";
    }
}