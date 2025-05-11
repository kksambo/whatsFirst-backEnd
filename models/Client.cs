using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }

        // Parameterless constructor
        public Client() { }

        // Constructor with parameters
        public Client(int id, string name, string phoneNumber)
        {
            Id = id;
            Name = name;
            PhoneNumber = phoneNumber;
        }
    }
