//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Kryptos.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserRegistrationOTP
    {
        public int Id { get; set; }
        public int USERID { get; set; }
        public string OTP { get; set; }
        public Nullable<int> Status { get; set; }
        public string Notes { get; set; }
        public string CreatedById { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string ModifiedById { get; set; }
        public System.DateTime ModifiedDate { get; set; }
    }
}
