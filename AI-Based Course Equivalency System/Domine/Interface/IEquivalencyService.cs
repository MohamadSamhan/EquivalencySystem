using Domine.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domine.Interface
{
    public interface IEquivalencyService
    {

        Task<EquivalencyRequestDto> CreateRequestAsync(int studentId, CreateEquivalencyRequestDto dto);// Create a new equivalency request
        Task<IEnumerable<EquivalencyRequestDto>> GetRequestsForDoctorAsync(); // Get all requests for a specific doctor
        Task<IEnumerable<EquivalencyRequestDto>> GetRequestsForStudentAsync(int studentId); // Get all requests for a specific student
        Task<bool> ApproveRequestAsync(int requestId, int doctorId); // Approve a specific request
        Task<bool> RejectRequestAsync(int requestId, int doctorId); // Reject a specific request
    }
}
    

