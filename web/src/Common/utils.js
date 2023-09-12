import {faker} from '@faker-js/faker';

// Generate a random nickname
function generateRandomNickname() {
  const adjective = faker.commerce.productAdjective();
  const noun = faker.commerce.productName();
  return `${adjective} ${noun}`;
}

export { generateRandomNickname };