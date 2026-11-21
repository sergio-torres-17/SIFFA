namespace SIFFA.Core.Models.Custom.Request.Employeees
{
    public enum CardCodeType
    {
        Qr = 1,
        Barcode = 2,
    }

    public enum CardFormat
    {
        Png = 1,
        Pdf = 2,
    }

    /// <summary>Datos que se imprimen en la tarjeta de invitación de un empleado.</summary>
    public class InvitationCardData
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = string.Empty;
        /// <summary>Adicionales (boletos) que recibe: nombre del tipo y cantidad.</summary>
        public List<InvitationCardAdd> Adds { get; set; } = new();
    }

    public record InvitationCardAdd(string Name, int Quantity);

    public record CardsZipResult(bool IsSuccess, string Message, byte[]? File = null, string FileName = "", int Count = 0);

    /// <summary>Avance de la generación de tarjetas: <paramref name="Done"/> de <paramref name="Total"/>.</summary>
    public record CardsProgress(int Done, int Total);
}
