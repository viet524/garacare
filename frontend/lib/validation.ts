const VN_PHONE_REGEX = /^(0|\+84)(3[2-9]|5[2568]|7[06-9]|8[1-689]|9[0-46-9])[0-9]{7}$/;

export function isValidVietnamesePhone(phone: string): boolean {
  return VN_PHONE_REGEX.test(phone.trim());
}
