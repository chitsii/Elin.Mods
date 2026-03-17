import unittest

from tools.drama import create_drama_excel


class DramaGeneratorRegistryTests(unittest.TestCase):
    def test_all_registered_dramas_have_v2_save_handlers(self):
        create_drama_excel.validate_drama_registry()

        for drama_id, save_fn in create_drama_excel.DRAMAS:
            self.assertTrue(
                callable(save_fn),
                f"Drama {drama_id} must have a callable v2 save handler",
            )


if __name__ == "__main__":
    unittest.main()
