using System;

namespace ProjetDotNet.Models
{
    public class Invitation
    {
        public int InvitationID { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Accepted { get; set; } = false;
        public int InviterUserID { get; set; }
        public int OrgID { get; set; }
        public int? TeamID { get; set; }
        public string Role { get; set; } = "MEMBRE";
        public DateTime DateCreation { get; set; } = DateTime.Now;

        

        //public static implicit operator Invitation(Invitation v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
