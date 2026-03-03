import { staffApi } from '@/infrastructure/api/staffApi';
import { businessUnitApi } from '@/infrastructure/api/businessUnitApi';
import { authApi } from '@/infrastructure/api/authApi';
import { chatApi } from '@/infrastructure/api/chatApi';
import { chatMessageApi } from '@/infrastructure/api/chatMessageApi';

import { GetStaffUseCase } from '@/application/use-cases/staff/GetStaffUseCase';
import { GetPersonsUseCase } from '@/application/use-cases/staff/GetPersonsUseCase';
import { CreateStaffUseCase } from '@/application/use-cases/staff/CreateStaffUseCase';
import { EnrollExistingUseCase } from '@/application/use-cases/staff/EnrollExistingUseCase';
import { UpdateStaffRoleUseCase } from '@/application/use-cases/staff/UpdateStaffRoleUseCase';
import { GetBusinessUnitsUseCase } from '@/application/use-cases/business-units/GetBusinessUnitsUseCase';
import { CreateBusinessUnitUseCase } from '@/application/use-cases/business-units/CreateBusinessUnitUseCase';
import { ChangePasswordUseCase } from '@/application/use-cases/auth/ChangePasswordUseCase';
import { GetChatWorkspaceUseCase } from '@/application/use-cases/chat/GetChatWorkspaceUseCase';
import { UpdateChatAccessUseCase } from '@/application/use-cases/chat/UpdateChatAccessUseCase';
import { GetMessagesUseCase } from '@/application/use-cases/chat/GetMessagesUseCase';
import { SendMessageUseCase } from '@/application/use-cases/chat/SendMessageUseCase';

export const container = {
  staff: {
    getAll:         new GetStaffUseCase(staffApi),
    getPersons:     new GetPersonsUseCase(staffApi),
    create:         new CreateStaffUseCase(staffApi),
    enrollExisting: new EnrollExistingUseCase(staffApi),
    updateRole:     new UpdateStaffRoleUseCase(staffApi),
  },
  businessUnits: {
    getAll:  new GetBusinessUnitsUseCase(businessUnitApi),
    create:  new CreateBusinessUnitUseCase(businessUnitApi),
  },
  auth: {
    changePassword: new ChangePasswordUseCase(authApi),
  },
  chat: {
    getWorkspace:  new GetChatWorkspaceUseCase(chatApi),
    updateAccess:  new UpdateChatAccessUseCase(chatApi),
    getMessages:   new GetMessagesUseCase(chatMessageApi),
    sendMessage:   new SendMessageUseCase(chatMessageApi),
  },
};
