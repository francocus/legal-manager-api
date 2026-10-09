using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;

namespace LegalManager.Presentation
{
    public static class Policies
    {
        public const string AdminsOnly = "AdminsOnly";

        public const string AdminOrLawyer = "AdminOrLawyer";

        public const string AllRoles = "AllRoles";
    }
}